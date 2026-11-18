using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;

namespace HonorControl;

// Capabilities come from the backend platform profile. The UI only hides what the
// backend refuses anyway; no control is enabled here that the firmware layer blocks.
public sealed partial class MainWindow
{
    private string _platformProfile = "";
    private string _platformDisplay = "", _platformDetail = "", _platformModel = "", _platformBios = "", _platformCpu = "";
    private bool _capHunter, _capFan, _capModeWrite, _capDiscrete, _capChargeWrite, _capColor, _capAudio, _capDisplayGamma, _capNetwork, _capFnWrite, _capDtt;
    private bool _platformSupported = true;

    private static bool Flag(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;

    private async Task LoadPlatformAsync()
    {
        try
        {
            var status = await _backend.PlatformStatusAsync();
            _platformProfile = status.Text("Profile", "");
            _platformSupported = string.Equals(_platformProfile, "FRI-HXX", StringComparison.Ordinal);
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
            if (!_platformSupported)
            {
                UnsupportedBanner.Title = L.T("设备未通过适配检查");
                UnsupportedBanner.Message = L.T("当前设备不在 Honor Control 的适配范围内：") + model + " / BIOS " + bios + L.T("。所有功能仅作展示，不会向设备写入任何设置。");
                UnsupportedBanner.IsOpen = true;
                AboutScopeText.Text = L.T("当前设备未通过适配检查。所有控制仅作展示，不会读取或写入设备。支持的机型为 HONOR MagicBook X14 2024（FRI-HXX / M1030 / BIOS 1.15 / 7640HS / Radeon 760M）。");
            }
            else if (_capDisplayGamma)
                AboutScopeText.Text = L.T("当前机型为 ") + model + " / BIOS " + bios + L.T("：已适配充电保护、性能模式与 Fn 设置，显示使用系统伽马调节；网络、音频、风扇与显卡控制未启用。");
            else
                AboutScopeText.Text = L.T("当前机型为 ") + model + " / BIOS " + bios + L.T("：读取已验证，写入尚未启用；色彩与音频未适配该机型。");
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
        if (!_capHunter)
        {
            HunterModeButton.Visibility = Visibility.Visible;
            HunterModeButton.IsEnabled = false;
        }
        else HunterModeButton.Visibility = Visibility.Visible;
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
            ChargeCustomButton.IsEnabled = false;
            ChargeProtectionToggle.IsEnabled = false;
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
        if (!_platformSupported) SetDisplayOnly(Navigation);
        if (!_capModeWrite && _platformSupported) Show(L.T("当前机型为只读档案：只显示传感器与固件状态，写入需逐项验证。"), InfoBarSeverity.Informational);
    }

    private static void SetDisplayOnly(DependencyObject node)
    {
        int count = VisualTreeHelper.GetChildrenCount(node);
        for (int index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(node, index);
            if (child is NavigationViewItem) continue;
            if (child is Button || child is ToggleButton || child is ToggleSwitch || child is ComboBox || child is CheckBox || child is Slider || child is TextBox || child is NumberBox)
                ((Control)child).IsEnabled = false;
            SetDisplayOnly(child);
        }
    }
}
