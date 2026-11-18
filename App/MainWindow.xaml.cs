using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using HonorControl.Shared;

namespace HonorControl;

public sealed partial class MainWindow : Window
{
    private readonly BackendClient _backend = new();
    private readonly ObservableCollection<string> _activity = [];
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };
    private readonly DispatcherTimer _noticeTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private bool _ready, _busy, _active = true, _closing, _quitRequested;
    private bool _syncingFn, _syncingDisplay;
    private bool _rhythmDesired;
    private TrayIcon? _tray;
    private int _mode = -1;
    private int _modeRevision;

    public MainWindow()
    {
        InitializeComponent();
        InitializeAbout();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "HonorControl.ico"));
        SystemBackdrop = new MicaBackdrop();
        Root.ActualThemeChanged += (_, _) => UpdateTitleBarTheme();
        UpdateTitleBarTheme();
        AppWindow.Resize(new SizeInt32(1220, 860));
        if (AppWindow.Presenter is OverlappedPresenter presenter) presenter.IsMaximizable = true;
        ActivityList.ItemsSource = _activity;
        _noticeTimer.Tick += (_, _) => { _noticeTimer.Stop(); Notice.IsOpen = false; };
        _tray = new TrayIcon(WinRT.Interop.WindowNative.GetWindowHandle(this), TrayAction);
        AppWindow.Closing += (_, args) => { if (!_quitRequested) { args.Cancel = true; AppWindow.Hide(); } };
        Root.Loaded += async (_, _) =>
        {
            double scale = Root.XamlRoot?.RasterizationScale ?? 1;
            var work = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
            int width = (int)Math.Min(1220 * scale, work.Width * .94);
            int height = (int)Math.Min(860 * scale, work.Height * .92);
            AppWindow.Resize(new SizeInt32(width, height));
            AppWindow.Move(new PointInt32(work.X + (work.Width - width) / 2, work.Y + (work.Height - height) / 2));
            _ready = true;
            await LoadPlatformAsync();
            ResidentStatusText.Text = AppPaths.Installed ? L.T("已安装 · 登录自动启动 · 后台保活已启用") : L.T("便携运行 · 关闭窗口后保留托盘");
            if (!_platformSupported)
            {
                if (AppPaths.BackgroundLaunch) AppWindow.Hide();
                if (!AppPaths.BackgroundLaunch) await ShowUnsupportedNoticeAsync();
            }
            else
            {
                await RunAsync(async () =>
                {
                    if (AppPaths.Installed || AppPaths.ExistingPipeName is not null)
                    {
                        UpdateFirmware(await _backend.HardwareAsync("firmware-status"));
                        UpdateCharge(await _backend.HardwareAsync("charge-status"));
                        UpdateKeyboardMode(await _backend.HardwareAsync("keyboard-status"));
                        await RefreshDisplayAsync();
                        await LoadRhythmPreferenceAsync();
                    }
                }, null);
                Log(L.T("应用已启动。"));
            }
            _timer.Start();
            if (AppPaths.BackgroundLaunch) AppWindow.Hide();
            if (!AppPaths.BackgroundLaunch && _platformSupported) await ShowStartupGuideAsync();
        };
        Activated += (_, e) => { _active = e.WindowActivationState != WindowActivationState.Deactivated; };
        _timer.Tick += async (_, _) =>
        {
            if (!_ready || _busy || _closing || !_platformSupported) return;
            if (_rhythmDesired && _capDisplayGamma)
            {
                try { await _backend.DisplayGammaAsync("display-gamma-rhythm-tick"); }
                catch (Exception ex) { Log(Friendly(ex)); }
            }
            if (!_active || !_backend.IsConnected) return;
            try
            {
                UpdateFirmware(await _backend.HardwareAsync("firmware-status"));
                UpdateCharge(await _backend.HardwareAsync("charge-status"));
            }
            catch (Exception ex) { Log(Friendly(ex)); }
        };
        Closed += async (_, _) =>
        {
            _closing = true; _timer.Stop();
            try { await _backend.DisplayGammaAsync("display-gamma-restore"); } catch { }
            _tray?.Dispose();
            await _backend.DisposeAsync();
        };
    }

    private async Task RunAsync(Func<Task> operation, string? success)
    {
        if (!_platformSupported) { Show(L.T("当前设备未通过适配检查，所有功能仅作展示。"), InfoBarSeverity.Warning); return; }
        if (_busy || _closing) return;
        _busy = true; BusyRing.IsActive = true; BusyRing.Visibility = Visibility.Visible;
        try
        {
            await operation();
            if (success is not null) { Show(success, InfoBarSeverity.Success); Log(success); }
        }
        catch (Exception ex)
        {
            string message = Friendly(ex);
            Show(message, InfoBarSeverity.Error); Log(message);
        }
        finally
        {
            _busy = false; BusyRing.IsActive = false; BusyRing.Visibility = Visibility.Collapsed;
            UpdateConnection(); UpdateSelections();
        }
    }
    private void Show(string message, InfoBarSeverity severity)
    {
        Notice.Message = L.T(message); Notice.Severity = severity; Notice.IsOpen = true;
        _noticeTimer.Stop(); _noticeTimer.Start();
    }
    private void Log(string message)
    {
        string line = DateTime.Now.ToString("HH:mm:ss") + "  ·  " + message;
        _activity.Insert(0, line); while (_activity.Count > 30) _activity.RemoveAt(_activity.Count - 1);
        HonorControl.Diagnostics.DiagnosticLog.Append(Path.Combine(AppPaths.UserState, "activity.log"), DateTimeOffset.Now.ToString("o") + " " + message + Environment.NewLine);
    }
    private static string Friendly(Exception ex)
    {
        if (ex is System.ComponentModel.Win32Exception { NativeErrorCode: 1223 }) return L.T("已取消管理员授权。");
        string text = ex.Message;
        if (text.Contains("AC power", StringComparison.OrdinalIgnoreCase)) return L.T("请连接电源，并确保电量至少为 20%。");
        if (text.Contains("not enabled for this platform", StringComparison.OrdinalIgnoreCase)) return L.T("该功能在当前机型尚未启用。");
        if (text.Contains("荣耀电脑管家", StringComparison.Ordinal)) return L.T("荣耀电脑管家性能服务仍在运行，请先退出电脑管家后重试。");
        if (text.Contains("拒绝访问", StringComparison.Ordinal)) return L.T("读取固件需要管理员权限，请点击“启用硬件控制”。");
        return L.T(text);
    }
    private void UpdateConnection()
    {
        ConnectLabel.Text = _backend.IsConnected ? L.T("硬件控制已启用") : L.T("启用硬件控制");
        ConnectButton.IsEnabled = !_backend.IsConnected && !_busy && _platformSupported;
    }
    private static string ModeName(int mode) => mode switch { 0 => L.T("平衡"), 1 => L.T("高能"), 3 => "HUNTER", _ => L.T("等待连接") };
    private void UpdateMode(int mode)
    {
        _mode = mode;
        CurrentModeText.Text = ModeName(mode);
        ModeFootnote.Text = !_capModeWrite ? L.T("该机型的性能档位写入尚未验证，当前只读取固件状态。")
            : mode is 0 or 1 ? L.T("已从设备固件读取确认") : L.T("连接后读取实际模式");
    }
    private void UpdateFirmware(JsonElement value)
    {
        UpdateMode(value.Number("ModeCode"));
        int temperature = value.Number("CpuCelsius");
        CpuTemperature.Text = temperature is >= 0 and <= 130 ? temperature + " °C" : "— °C";
        UpdateSelections();
    }
    private void UpdateSelections()
    {
        SmartModeButton.IsChecked = _mode == 0;
        PerformanceModeButton.IsChecked = _mode == 1;
        HunterModeButton.IsChecked = false;
        SmartSelected.Visibility = _mode == 0 ? Visibility.Visible : Visibility.Collapsed;
        PerformanceSelected.Visibility = _mode == 1 ? Visibility.Visible : Visibility.Collapsed;
        HunterSelected.Visibility = Visibility.Collapsed;
    }
    private void UpdateCharge(JsonElement status)
    {
        int start = status.Number("ChargeStartPercent"), stop = status.Number("ChargeStopPercent");
        int battery = status.Number("BatteryPercent", -1);
        bool ac = status.ValueKind == JsonValueKind.Object && status.TryGetProperty("AcOnline", out var online) && online.ValueKind == JsonValueKind.True;
        string power = ac ? L.T("已接通电源") : L.T("电池供电");
        PowerSourceText.Text = power;
        PowerSourceHint.Text = battery >= 0 ? L.F($"电池 {battery}%") : L.T("已读取电源状态");
        ChargeStatus.Text = battery >= 0
            ? L.F($"电池 {battery}% · {power} · {start}% 开始充电 / {stop}% 停止充电")
            : L.F($"固件读数 · {start}% 开始充电 / {stop}% 停止充电");
        UpdateChargeSelection(start, stop);
    }
    private async void Connect_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        await _backend.ConnectAsync();
        UpdateFirmware(await _backend.HardwareAsync("firmware-status"));
        UpdateCharge(await _backend.HardwareAsync("charge-status"));
        UpdateKeyboardMode(await _backend.HardwareAsync("keyboard-status"));
    }, L.T("硬件控制已启用。本次窗口会话无需重复管理员授权。"));
    private async void Mode_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) { UpdateSelections(); return; }
        await ApplyModeAsync((string)((ToggleButton)sender).Tag);
    }
    private async Task ApplyModeAsync(string command)
    {
        await RunAsync(async () => { _modeRevision++; UpdateFirmware(await _backend.HardwareAsync(command)); }, L.T("切换成功"));
    }
    private async void ReadCharge_Click(object sender, RoutedEventArgs e) { _chargeCustomEditing = false; await RunAsync(async () => UpdateCharge(await _backend.HardwareAsync("charge-status")), null); }
    private async void Charge_Click(object sender, RoutedEventArgs e)
    {
        _chargeCustomEditing = false;
        string command = (string)((ToggleButton)sender).Tag;
        await RunAsync(async () =>
        {
            try { UpdateCharge(await _backend.HardwareAsync(command)); }
            catch { UpdateCharge(await _backend.HardwareAsync("charge-status")); throw; }
        }, L.T("充电阈值已从固件读回。"));
    }

    private async Task RefreshDisplayAsync()
    {
        var status = await _backend.DisplayGammaAsync("display-gamma-status");
        string effect = status.Text("EffectStyle", "off");
        bool active = status.ValueKind == JsonValueKind.Object && status.TryGetProperty("Active", out var value) && value.ValueKind == JsonValueKind.True;
        bool rhythm = status.ValueKind == JsonValueKind.Object && status.TryGetProperty("RhythmActive", out var rhythmValue) && rhythmValue.ValueKind == JsonValueKind.True;
        DisplayModeSummary.Text = rhythm ? L.T("当前显示 · 亮度节律已启用") : active ? L.T("当前显示 · 系统伽马调节已启用") : L.T("当前显示 · 系统原色");
        DisplayEffectPicker.SelectedIndex = effect == "eye" ? 1 : effect == "reading" ? 2 : 0;
        TemperatureSlider.Value = status.Number("Temperature", 0);
        DisplayEffectStatus.Text = !active ? L.T("显示效果已关闭") : L.T("系统伽马表已读回");
        DisplayVerificationText.Text = active ? L.T("Windows 系统伽马表已应用，可随时恢复原色。") : L.T("当前使用 Windows 原始伽马表。");
        _syncingDisplay = true;
        try { BrightnessRhythmToggle.IsOn = rhythm; }
        finally { _syncingDisplay = false; }
        _rhythmDesired = rhythm;
        RhythmStatusText.Text = rhythm ? L.T("亮度节律运行中") : L.T("亮度节律未启用");
    }
    private static string RhythmPreference => Path.Combine(AppPaths.UserState, "brightness-rhythm-enabled.txt");
    private async Task LoadRhythmPreferenceAsync()
    {
        if (!_capDisplayGamma) return;
        _rhythmDesired = File.Exists(RhythmPreference) && File.ReadAllText(RhythmPreference).Trim() == "1";
        if (!_rhythmDesired) return;
        try
        {
            await _backend.DisplayGammaAsync("display-gamma-rhythm-start");
            await RefreshDisplayAsync();
        }
        catch (Exception ex)
        {
            _rhythmDesired = false;
            Log(Friendly(ex));
        }
    }
    private async void BrightnessRhythm_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_ready || _syncingDisplay || _busy) return;
        bool enabled = BrightnessRhythmToggle.IsOn;
        await RunAsync(async () =>
        {
            await _backend.DisplayGammaAsync(enabled ? "display-gamma-rhythm-start" : "display-gamma-rhythm-stop");
            _rhythmDesired = enabled;
            Directory.CreateDirectory(AppPaths.UserState);
            File.WriteAllText(RhythmPreference, enabled ? "1" : "0");
            await RefreshDisplayAsync();
        }, enabled ? L.T("亮度节律已启用。") : L.T("亮度节律已关闭。"));
    }
    private async void ApplyDisplayEffect_Click(object sender, RoutedEventArgs e)
    {
        string effect = DisplayEffectPicker.SelectedIndex == 1 ? "eye" : DisplayEffectPicker.SelectedIndex == 2 ? "reading" : "off";
        string value = ((int)TemperatureSlider.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
        await RunAsync(async () => { await _backend.DisplayGammaAsync("display-gamma-apply", effect, value); await RefreshDisplayAsync(); }, L.T("显示效果已应用并读回。"));
    }
    private async void RestoreDisplay_Click(object sender, RoutedEventArgs e)
    {
        if (_rhythmDesired)
        {
            _rhythmDesired = false;
            Directory.CreateDirectory(AppPaths.UserState);
            File.WriteAllText(RhythmPreference, "0");
        }
        await RunAsync(async () => { await _backend.DisplayGammaAsync("display-gamma-restore"); await RefreshDisplayAsync(); }, L.T("已恢复 Windows 原始伽马表。"));
    }
    private async void RefreshDisplay_Click(object sender, RoutedEventArgs e) => await RunAsync(RefreshDisplayAsync, L.T("已重新读取显示状态。"));

    private void UpdateKeyboardMode(JsonElement status)
    {
        int code = status.Number("FnStateCode", 0);
        _syncingFn = true;
        try { FnRowModePicker.SelectedIndex = code == 2 ? 1 : 0; }
        finally { _syncingFn = false; }
        KeyboardFnStateText.Text = code == 2 ? L.T("当前：标准 F1–F12 优先") : L.T("当前：键帽图标功能优先");
    }
    private async void ReadKeyboardMode_Click(object sender, RoutedEventArgs e) => await RunAsync(async () => UpdateKeyboardMode(await _backend.HardwareAsync("keyboard-status")), null);
    private async void FnRowMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || _syncingFn || !_capFnWrite || _busy) return;
        string command = FnRowModePicker.SelectedIndex == 1 ? "keyboard-fkeys" : "keyboard-icons";
        await RunAsync(async () => UpdateKeyboardMode(await _backend.HardwareAsync(command)), L.T("Fn 行模式已写入并读回。"));
    }

    private async void Navigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (!_ready || args.SelectedItem is not NavigationViewItem item) return;
        string tag = item.Tag?.ToString() ?? "performance";
        PerformancePage.Visibility = tag == "performance" ? Visibility.Visible : Visibility.Collapsed;
        DisplayPage.Visibility = tag == "display" ? Visibility.Visible : Visibility.Collapsed;
        SettingsPage.Visibility = tag == "settings" ? Visibility.Visible : Visibility.Collapsed;
        AboutPage.Visibility = tag == "about" ? Visibility.Visible : Visibility.Collapsed;
        PageHeading.Text = tag switch
        {
            "display" => L.T("显示与色彩"),
            "settings" => L.T("设置与记录"),
            "about" => L.T("关于"),
            _ => L.T("性能与充电")
        };
        AnimatePage(tag switch
        {
            "display" => DisplayPage,
            "settings" => SettingsPage,
            "about" => AboutPage,
            _ => PerformancePage
        });
        if (tag == "display" && _platformSupported) await RunAsync(RefreshDisplayAsync, null);
    }
    private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready) return;
        Root.RequestedTheme = ThemePicker.SelectedIndex switch { 1 => ElementTheme.Light, 2 => ElementTheme.Dark, _ => ElementTheme.Default };
    }
    private void UpdateTitleBarTheme()
    {
        bool dark = Root.ActualTheme == ElementTheme.Dark;
        var bar = AppWindow.TitleBar;
        bar.ButtonBackgroundColor = Colors.Transparent;
        bar.ButtonInactiveBackgroundColor = Colors.Transparent;
        bar.ButtonForegroundColor = dark ? Colors.White : Colors.Black;
        bar.ButtonInactiveForegroundColor = dark ? Windows.UI.Color.FromArgb(255, 130, 138, 150) : Windows.UI.Color.FromArgb(255, 100, 108, 120);
        bar.ButtonHoverBackgroundColor = dark ? Windows.UI.Color.FromArgb(255, 55, 60, 69) : Windows.UI.Color.FromArgb(255, 223, 228, 236);
        bar.ButtonHoverForegroundColor = dark ? Colors.White : Colors.Black;
    }
    private void OpenLogs_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(AppPaths.UserState); Process.Start(new ProcessStartInfo(AppPaths.UserState) { UseShellExecute = true });
    }
    public void ShowMainWindow()
    {
        try { File.Delete(RuntimeIdentity.PauseFile); } catch { }
        AppWindow.Show(); Activate();
        if (_ready) _ = ShowStartupGuideAsync();
    }
    public void RequestExit() => TrayAction("quit");
    private async void TrayAction(string action)
    {
        if (action == "show") { ShowMainWindow(); return; }
        if (action == "quit")
        {
            Directory.CreateDirectory(AppPaths.UserState); File.WriteAllText(RuntimeIdentity.PauseFile, "Paused until next login or manual launch.");
            _quitRequested = true; _closing = true;
            Close(); Application.Current.Exit(); return;
        }
        if (action.StartsWith("firmware-", StringComparison.Ordinal)) await ApplyModeAsync(action);
    }
}
