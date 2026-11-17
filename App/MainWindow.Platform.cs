using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Text.Json;

namespace HonorControl;

// Capabilities come from the backend platform profile. The UI only hides what the
// backend refuses anyway; no control is enabled here that the firmware layer blocks.
public sealed partial class MainWindow
{
    private string _platformProfile = "";
    private string _platformDisplay = "", _platformDetail = "", _platformModel = "", _platformBios = "", _platformCpu = "";
    private bool _capHunter, _capFan, _capModeWrite, _capDiscrete, _capChargeWrite, _capColor, _capAudio, _capDisplayGamma, _capNetwork, _capFnWrite, _capDtt;

    private static bool Flag(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;

    private async Task LoadPlatformAsync()
    {
        try
        {
            var status = await _backend.PlatformStatusAsync();
            _platformProfile = status.Text("Profile", "");
            _platformDisplay = status.Text("Display", "");
            _platformDetail = status.Text("Detail", "");
            _platformModel = status.Text("Model", "");
            _platformBios = status.Text("Bios", "");
            _platformCpu = status.Text("Cpu", "").Trim();
            _capHunter = Flag(status, "Hunter");
            _capFan = Flag(status, "FanPreset");
            _capModeWrite = Flag(status, "ModeWrite");
            _capDiscrete = Flag(status, "DiscreteGpu");
            _capChargeWrite = Flag(status, "ChargeWrite");
            _capColor = Flag(status, "ColorLut");
            _capAudio = Flag(status, "Audio");
            _capDisplayGamma = Flag(status, "DisplayGamma");
            _capNetwork = Flag(status, "Network");
            _capFnWrite = Flag(status, "FnWrite");
            _capDtt = Flag(status, "Dtt");
        }
        catch (Exception ex)
        {
            Log(L.T("读取平台档案失败：") + Friendly(ex));
            return;
        }
        ApplyPlatformCapabilities();
    }

    private void ApplyPlatformCapabilities()
    {
        if (_platformProfile.Length > 0)
        {
            string model = _platformModel.Length > 0 ? _platformModel : _platformProfile;
            string name = _platformDisplay.Length > 0 ? _platformDisplay : model;
            string bios = _platformBios.Length > 0 ? _platformBios : "—";
            FooterModel.Text = name.ToUpperInvariant();
            FooterBios.Text = model + " · BIOS " + bios;
            AboutDevice.Text = name + " · " + model + " · BIOS " + bios;
            AboutDeviceDetail.Text = _platformDetail.Length > 0 ? _platformDetail : _platformCpu;
            if (_platformProfile != "DRA-XX")
                AboutScopeText.Text = _capDisplayGamma
                    ? L.T("当前机型为 ") + model + " / BIOS " + bios + L.T("：已适配充电保护与性能模式，显示使用系统伽马调节；网络、音频、风扇与显卡控制未启用。")
                    : L.T("当前机型为 ") + model + " / BIOS " + bios + L.T("：读取已验证，写入尚未启用；色彩与音频未适配该机型。");
        }
        if (_tray is not null)
        {
            _tray.CanMode = _capModeWrite;
            _tray.CanHunter = _capHunter;
            _tray.CanColor = _capColor;
        }
        if (!_capFan)
        {
            FanPresetRow.Visibility = Visibility.Collapsed;
            FanAvailability.Text = L.T("该机型不提供风扇预设写入，仅显示实时风扇转速。");
        }
        if (!_capHunter) HunterModeButton.Visibility = Visibility.Collapsed;
        if (!_capModeWrite)
        {
            SmartModeButton.IsEnabled = false;
            PerformanceModeButton.IsEnabled = false;
            ModeFootnote.Text = L.T("该机型的性能档位写入尚未验证，当前只读取固件状态。");
        }
        if (!_capDiscrete) GpuCard.Visibility = Visibility.Collapsed;
        if (!_capDtt) AutomationCard.Visibility = Visibility.Collapsed;
        if (!_capChargeWrite)
        {
            ChargeHome.IsEnabled = false;
            ChargeOffice.IsEnabled = false;
            ChargeTravel.IsEnabled = false;
            ChargeStatus.Text = L.T("该机型的充电阈值写入尚未启用。");
        }
        if (!_capColor)
        {
            ColorProfileCard.Visibility = Visibility.Collapsed;
            if (!_capDisplayGamma)
            {
                DisplayPage.Visibility = Visibility.Collapsed;
                NavDisplay.Visibility = Visibility.Collapsed;
            }
        }
        if (_capDisplayGamma && !_capColor)
        {
            ColorModeGrid.Visibility = Visibility.Collapsed;
            ColorStatusCard.Visibility = Visibility.Collapsed;
            ColorWatchCard.Visibility = Visibility.Collapsed;
        }
        if (!_capNetwork)
        {
            NavNetwork.Visibility = Visibility.Collapsed;
            NetworkPage.Visibility = Visibility.Collapsed;
        }
        if (!_capFnWrite) FnCard.Visibility = Visibility.Collapsed;
        if (!_capAudio) NavAudio.Visibility = Visibility.Collapsed;
        if (!_capModeWrite && _platformProfile.Length > 0) Show(L.T("当前机型为只读档案：只显示传感器与固件状态，写入需逐项验证。"), InfoBarSeverity.Informational);
    }
}
