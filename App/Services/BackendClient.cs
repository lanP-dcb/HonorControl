using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using HonorControl.Shared;

namespace HonorControl;

internal static class AppPaths
{
    public static string Backend => Path.Combine(AppContext.BaseDirectory, "Backend");
    public static string UserState => RuntimeIdentity.UserState;
    public static string DeviceState => RuntimeIdentity.DeviceState;
    public static bool Installed => File.Exists(Path.Combine(AppContext.BaseDirectory, "installed.json"));
    public static bool BackgroundLaunch => Environment.GetCommandLineArgs().Contains("--background");
    public static string? ExistingPipeName
    {
        get
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--broker-pipe");
            return index >= 0 && index + 1 < args.Length && System.Text.RegularExpressions.Regex.IsMatch(args[index + 1], @"^HonorControl\.[a-fA-F0-9]{32}$") ? args[index + 1] : null;
        }
    }
}

internal sealed class BackendClient : IAsyncDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1);
    private readonly SemaphoreSlim _requestLock = new(1);
    private readonly SemaphoreSlim _displayLock = new(1);
    private NamedPipeClientStream? _pipe;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private bool _existingPipeUsed;
    public bool IsConnected => _pipe?.IsConnected == true;

    public async Task ConnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (IsConnected) return;
            if (AppPaths.Installed)
            {
                bool brokerStarted = false;
                if (!BrokerRunning())
                {
                    await StartResidentBrokerAsync();
                    brokerStarted = true;
                }
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    try
                    {
                        await ConnectResidentAsync(1500);
                        return;
                    }
                    catch (TimeoutException) { }
                    catch (IOException) { }
                    catch (OperationCanceledException) { }
                    await DisconnectAsync();
                    if (!brokerStarted && attempt == 10 && !BrokerRunning())
                    {
                        await StartResidentBrokerAsync();
                        brokerStarted = true;
                    }
                    await Task.Delay(500);
                }
                throw new TimeoutException(L.T("无法连接管理员控制会话，请重新打开 Honor Control 后重试。"));
            }
            string name;
            if (!_existingPipeUsed && AppPaths.ExistingPipeName is string existing)
            {
                name = existing; _existingPipeUsed = true;
            }
            else
            {
                name = "HonorControl." + Guid.NewGuid().ToString("N");
                string broker = Path.Combine(AppContext.BaseDirectory, "HonorControl.Broker.exe");
                var start = new ProcessStartInfo(broker) { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = AppContext.BaseDirectory };
                start.ArgumentList.Add("--pipe"); start.ArgumentList.Add(name);
                using var process = await Task.Run(() => Process.Start(start)) ?? throw new InvalidOperationException(L.T("无法启动管理员控制会话。"));
            }
            _pipe = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await _pipe.ConnectAsync(45000);
            _reader = new(_pipe, new UTF8Encoding(false), false, 4096, true);
            _writer = new(_pipe, new UTF8Encoding(false), 4096, true) { AutoFlush = true };
            await ExchangeAsync(new("hello"));
        }
        catch { await DisconnectAsync(); throw; }
        finally { _connectionLock.Release(); }
    }

    public async Task<JsonElement> HardwareAsync(string command)
    {
        await ConnectAsync();
        return await ExchangeAsync(new("firmware", command));
    }

    public async Task<JsonElement> DisplayGammaAsync(string command, params string[] arguments)
    {
        var allowed = new[] { "display-gamma-status", "display-gamma-apply", "display-gamma-restore", "display-gamma-rhythm-start", "display-gamma-rhythm-stop", "display-gamma-rhythm-tick" };
        if (!allowed.Contains(command)) throw new ArgumentException(L.T("不支持的显示调节操作。"));
        await _displayLock.WaitAsync();
        try { return await RunStandaloneAsync(command, arguments); }
        finally { _displayLock.Release(); }
    }

    // Platform detection needs no elevation, so the UI can size itself before any admin session exists.
    public async Task<JsonElement> PlatformStatusAsync()
    {
        await _displayLock.WaitAsync();
        try { return await RunStandaloneAsync("platform-status"); }
        finally { _displayLock.Release(); }
    }

    private async Task ConnectResidentAsync(int timeoutMilliseconds)
    {
        var pipe = new NamedPipeClientStream(".", RuntimeIdentity.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        try
        {
            await pipe.ConnectAsync(timeoutMilliseconds);
        }
        catch
        {
            await pipe.DisposeAsync();
            throw;
        }
        _pipe = pipe;
        _reader = new(_pipe, new UTF8Encoding(false), false, 4096, true);
        _writer = new(_pipe, new UTF8Encoding(false), 4096, true) { AutoFlush = true };
        await ExchangeAsync(new("hello"));
    }

    private static bool BrokerRunning() => Process.GetProcessesByName("HonorControl.Broker").Length > 0;

    private static async Task StartResidentBrokerAsync()
    {
        bool started = false;
        string schtasks = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe");
        if (File.Exists(schtasks))
        {
            var start = new ProcessStartInfo(schtasks) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add("/Run");
            start.ArgumentList.Add("/TN");
            start.ArgumentList.Add(RuntimeIdentity.AgentTask);
            using var process = Process.Start(start);
            if (process is not null)
            {
                await process.WaitForExitAsync();
                started = process.ExitCode == 0;
            }
        }
        if (started) return;
        var broker = Path.Combine(AppContext.BaseDirectory, "HonorControl.Broker.exe");
        var fallback = new ProcessStartInfo(broker) { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = AppContext.BaseDirectory };
        fallback.ArgumentList.Add("--resident");
        fallback.ArgumentList.Add("--allow-writes");
        Process.Start(fallback);
    }

    private async Task<JsonElement> ExchangeAsync(ControlRequest request)
    {
        await _requestLock.WaitAsync();
        try
        {
            if (_writer is null || _reader is null) throw new InvalidOperationException(L.T("管理员会话尚未连接。"));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(40));
            await _writer.WriteLineAsync(Wire.Encode(request));
            string? line = await _reader.ReadLineAsync(timeout.Token);
            if (line is null) throw new IOException(L.T("管理员控制会话已结束，请重新启用硬件控制。"));
            var result = Wire.Decode<ControlResponse>(line);
            if (!result.Success) throw new InvalidOperationException(result.Error ?? L.T("控制操作未完成。"));
            return result.Data?.Clone() ?? JsonSerializer.SerializeToElement(Array.Empty<object>());
        }
        catch (IOException) { await DisconnectAsync(); throw; }
        catch (OperationCanceledException) { await DisconnectAsync(); throw; }
        finally { _requestLock.Release(); }
    }

    private async Task<JsonElement> RunStandaloneAsync(string command, params string[] arguments)
    {
        var start = new ProcessStartInfo(Path.Combine(AppPaths.Backend, "HonorStandalone.exe")) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = AppPaths.Backend, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        start.ArgumentList.Add(command);
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException(L.T("无法启动设备控制程序。"));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
        try { await process.WaitForExitAsync(timeout.Token); } catch { try { process.Kill(true); } catch { } throw; }
        string text = (await stdout).Trim().TrimStart('\uFEFF');
        string error = (await stderr).Trim();
        if (process.ExitCode != 0)
        {
            try { var result = JsonDocument.Parse(error.Length > 0 ? error : text).RootElement; if (result.TryGetProperty("Error", out var message)) throw new InvalidOperationException(message.GetString()); } catch (JsonException) { }
            throw new InvalidOperationException(error.Length > 0 ? error : text);
        }
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    private async Task DisconnectAsync()
    {
        try { _writer?.Dispose(); } catch (IOException) { } catch (ObjectDisposedException) { }
        _writer = null;
        _reader?.Dispose(); _reader = null;
        if (_pipe is not null) { await _pipe.DisposeAsync(); _pipe = null; }
    }

    public async ValueTask DisposeAsync()
    {
        try { if (IsConnected) await ExchangeAsync(new("exit")); } catch { }
        await DisconnectAsync();
    }
}
