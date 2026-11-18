using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;

namespace HonorControl;

// Capabilities come from the backend platform profile. The UI never enables a
// control that the firmware layer would refuse.
public sealed partial class MainWindow
{
    private string _platformProfile = "";
    private string _platformDisplay = "", _platformDetail = "", _platformModel = "", _platformBios = "", _platformCpu = "";
    private bool _capModeWrite, _capChargeWrite, _capDisplayGamma, _capFnWrite;
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
            _capModeWrite = Flag(status, "ModeWrite");
            _capChargeWrite = Flag(status, "ChargeWrite");
            _capDisplayGamma = Flag(status, "DisplayGamma");
            _capFnWrite = Flag(status, "FnWrite");
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
            else
                AboutScopeText.Text = L.T("当前机型为 ") + model + " / BIOS " + bios + L.T("：已适配充电保护、智能/高能模式、Fn 与 Windows 系统伽马显示。");
        }
        if (_tray is not null) _tray.CanMode = _capModeWrite;
        if (!_capModeWrite)
        {
            SmartModeButton.IsEnabled = false;
            PerformanceModeButton.IsEnabled = false;
            ModeFootnote.Text = L.T("该机型的性能档位写入尚未验证，当前只读取固件状态。");
        }
        if (!_capChargeWrite)
        {
            ChargeHome.IsEnabled = false;
            ChargeOffice.IsEnabled = false;
            ChargeTravel.IsEnabled = false;
            ChargeCustomButton.IsEnabled = false;
            ChargeProtectionToggle.IsEnabled = false;
            ChargeStatus.Text = L.T("该机型的充电阈值写入尚未启用。");
        }
        if (!_capDisplayGamma)
        {
            DisplayPage.Visibility = Visibility.Collapsed;
            NavDisplay.Visibility = Visibility.Collapsed;
        }
        if (!_capFnWrite) FnCard.Visibility = Visibility.Collapsed;
        if (!_platformSupported) SetDisplayOnly(Navigation);
        if (!_capModeWrite && _platformSupported) Show(L.T("当前机型为只读档案：只显示传感器与固件状态。"), InfoBarSeverity.Informational);
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
