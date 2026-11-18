using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace HonorControl;

public sealed partial class MainWindow
{
    private GlobalHotkeys? _hotkeys;
    private HotkeyChord _draftPerformance = HotkeySettings.Default.Performance, _draftRefresh = HotkeySettings.Default.RefreshRate;
    private bool _syncingHotkeys, _capturingHotkey;
    private static string HotkeyFile => Path.Combine(AppPaths.UserState, "hotkeys.json");
    [StructLayout(LayoutKind.Sequential)] private struct HotkeyPowerStatus { public byte Ac, Flags, Percent, Saver; public uint Life, FullLife; }
    [DllImport("kernel32.dll")] private static extern bool GetSystemPowerStatus(out HotkeyPowerStatus power);
    [DllImport("user32.dll")] private static extern short GetKeyState(int key);

    private void LoadHotkeys()
    {
        try
        {
            _hotkeys = new GlobalHotkeys(WinRT.Interop.WindowNative.GetWindowHandle(this), action => { DispatcherQueue.TryEnqueue(() => HotkeyAction(action)); });
            var settings = HotkeySettings.Default;
            if (File.Exists(HotkeyFile))
            {
                try { settings = JsonSerializer.Deserialize<HotkeySettings>(File.ReadAllText(HotkeyFile)) ?? settings; settings.Validate(); }
                catch (Exception ex) { settings = HotkeySettings.Default; Log(L.T("快捷键配置无法读取，已使用默认组合键。") + ex.Message); }
            }
            _hotkeys.Configure(settings, strict: false);
            SyncHotkeyControls();
        }
        catch (Exception ex) { HotkeyStatusText.Text = Friendly(ex); }
    }
    private void SyncHotkeyControls()
    {
        if (_hotkeys is null) return;
        _syncingHotkeys = true;
        try
        {
            var value = _hotkeys.Configuration;
            HotkeysEnabledSwitch.IsOn = value.Enabled;
            _draftPerformance = value.Performance; _draftRefresh = value.RefreshRate;
            PerformanceHotkeyBox.Text = value.Performance.ToString();
            HotkeyStatusText.Text = _hotkeys.Status;
        }
        finally { _syncingHotkeys = false; }
    }
    private void HotkeyDraft_Changed(object sender, RoutedEventArgs e)
    {
        if (!_syncingHotkeys && _hotkeys is not null) HotkeyStatusText.Text = L.T("快捷键设置尚未保存。");
    }
    private void CaptureHotkey_GotFocus(object sender, RoutedEventArgs e)
    {
        _capturingHotkey = true;
        _hotkeys?.Suspend();
        HotkeyStatusText.Text = L.T("按下新的组合键；Esc 取消录入，Tab 移到下一项。");
    }
    private void CaptureHotkey_LostFocus(object sender, RoutedEventArgs e)
    {
        _capturingHotkey = false;
        _hotkeys?.Resume();
        if (_hotkeys is not null) HotkeyStatusText.Text = L.T("录入结果尚未保存。当前生效：") + _hotkeys.Status;
    }
    private void CaptureHotkey_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (sender is not TextBox box || _hotkeys is null) return;
        uint key = (uint)e.Key;
        if (key == 9) return;
        e.Handled = true;
        if (key is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C || key is >= 0xA0 and <= 0xA5 || e.KeyStatus.WasKeyDown) return;
        if (key == 0x1B)
        {
            _draftPerformance = _hotkeys.Configuration.Performance; box.Text = _draftPerformance.ToString();
            SaveHotkeysButton.Focus(FocusState.Programmatic);
            return;
        }
        uint modifiers = 0;
        if (GetKeyState(0x12) < 0) modifiers |= 1;
        if (GetKeyState(0x11) < 0) modifiers |= 2;
        if (GetKeyState(0x10) < 0) modifiers |= 4;
        if (GetKeyState(0x5B) < 0 || GetKeyState(0x5C) < 0) modifiers |= 8;
        var chord = new HotkeyChord(modifiers, key);
        try
        {
            chord.Validate();
            _draftPerformance = chord;
            box.Text = chord.ToString();
            HotkeyStatusText.Text = L.T("已录入 ") + chord + L.T("，点击保存后生效。");
        }
        catch (Exception ex) { HotkeyStatusText.Text = Friendly(ex); }
    }
    private void SaveHotkeys_Click(object sender, RoutedEventArgs e) => SaveHotkeys(new(HotkeysEnabledSwitch.IsOn, _draftPerformance, _draftRefresh));
    private void ResetHotkeys_Click(object sender, RoutedEventArgs e) => SaveHotkeys(HotkeySettings.Default);
    private void SaveHotkeys(HotkeySettings settings)
    {
        if (_hotkeys is null) return;
        var original = _hotkeys.Configuration;
        try
        {
            _hotkeys.Configure(settings);
            try
            {
                Directory.CreateDirectory(AppPaths.UserState);
                File.WriteAllText(HotkeyFile + ".tmp", JsonSerializer.Serialize(settings)); File.Move(HotkeyFile + ".tmp", HotkeyFile, true);
            }
            catch { _hotkeys.Configure(original, strict: false); throw; }
            SyncHotkeyControls(); Log(L.T("全局快捷键已保存。") + _hotkeys.Status);
        }
        catch (Exception ex) { HotkeyStatusText.Text = Friendly(ex); }
    }

    private async void HotkeyAction(string action)
    {
        if (!_ready || _busy || _closing) return;
        await RunAsync(async () =>
        {
            try
            {
                if (action == "cycle-performance")
                {
                    var current = await _backend.HardwareAsync("firmware-status");
                    if (!GetSystemPowerStatus(out var power) || power.Ac > 1) throw new InvalidOperationException(L.T("无法确认供电状态，请稍后重试。"));
                    bool barrel = current.TryGetProperty("BarrelJackPresent", out var jack) && jack.GetBoolean();
                    double pd = current.TryGetProperty("PdAdapterWatts", out var watts) ? watts.GetDouble() : 0;
                    int mode = PerformanceCycle.Next(current.Number("ModeCode"), power.Ac == 1, power.Percent, barrel, pd);
                    if (mode == current.Number("ModeCode") && mode == 0)
                        throw new InvalidOperationException(PerformanceCycle.UnavailableReason(1, power.Ac == 1, power.Percent, barrel, pd) ?? L.T("当前供电条件只支持智能模式。"));
                    _modeRevision++;
                    if (mode == 3) _scenePower?.Restore();
                    string command = mode switch { 0 => "firmware-smart", 1 => "firmware-performance", _ => "firmware-hunter" };
                    UpdateFirmware(await _backend.HardwareAsync(command));
                    PresentModeOverlay(_mode);
                    Log(L.T("全局快捷键切换性能模式：") + ModeName(_mode));
                }
                else if (action == "cycle-refresh")
                {
                    var result = await Task.Run(DisplayRefreshRate.Cycle);
                    await CompleteRefreshRateChangeAsync(result);
                }
            }
            catch (Exception ex) { _overlay.Present(L.T("切换未完成"), Friendly(ex), OverlayIcon.Warning); throw; }
        }, L.T("切换成功"));
    }
    private async Task CompleteRefreshRateChangeAsync(DisplayRefreshRate.Snapshot result)
    {
        _overlay.Present($"{result.Hertz:0.##} Hz", L.T("内屏刷新率"), OverlayIcon.RefreshRate);
        Log(L.F($"切换内屏刷新率：{result.Hertz:0.##} Hz，{result.Width} × {result.Height}。"));
        if (!_watcherDesired || !File.Exists(Path.Combine(AppPaths.DeviceState, "last-color-mode.txt"))) return;
        try
        {
            await _backend.ColorAsync("color-reapply");
            if (!_closing && DisplayPage.Visibility == Visibility.Visible) await RefreshColorAsync();
        }
        catch (Exception ex) { Log(L.T("刷新率已切换，恢复色彩时发生错误：") + Friendly(ex)); }
    }
}
