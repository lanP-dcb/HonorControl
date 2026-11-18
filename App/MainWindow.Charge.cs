using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HonorControl;

public sealed partial class MainWindow
{
    private int _chargeStart = 40, _chargeStop = 70;
    private bool _chargeSyncing;

    private void UpdateChargeSelection(int start, int stop)
    {
        _chargeStart = start;
        _chargeStop = stop;
        _chargeSyncing = true;
        try
        {
            bool protection = !(start == 0 && stop == 100);
            ChargeProtectionToggle.IsOn = protection;
            ChargeHome.IsChecked = (start, stop) == (40, 70);
            ChargeOffice.IsChecked = (start, stop) == (70, 90);
            ChargeTravel.IsChecked = (start, stop) == (95, 100);
            bool custom = protection && !ChargeHome.IsChecked.GetValueOrDefault() && !ChargeOffice.IsChecked.GetValueOrDefault() && !ChargeTravel.IsChecked.GetValueOrDefault();
            ChargeCustomButton.IsChecked = custom;
            ChargeCustomPanel.Visibility = custom ? Visibility.Visible : Visibility.Collapsed;
            if (custom)
            {
                ChargeStartBox.Value = start;
                ChargeStopBox.Value = stop;
            }
        }
        finally { _chargeSyncing = false; }
    }

    private async void ChargeProtection_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_ready || _chargeSyncing || !_capChargeWrite) return;
        bool on = ChargeProtectionToggle.IsOn;
        await ApplyChargeAsync(on ? 40 : 0, on ? 70 : 100, on ? L.T("充电保护已启用，默认使用家用 40–70%。") : L.T("充电保护已关闭，恢复为 0–100%，充满后停止。"));
    }

    private void ChargeCustom_Selected(object sender, RoutedEventArgs e)
    {
        if (!_capChargeWrite) return;
        _chargeSyncing = true;
        try
        {
            ChargeHome.IsChecked = false;
            ChargeOffice.IsChecked = false;
            ChargeTravel.IsChecked = false;
            ChargeCustomButton.IsChecked = true;
            ChargeCustomPanel.Visibility = Visibility.Visible;
            ChargeStartBox.Value = Math.Max(20, _chargeStart);
            ChargeStopBox.Value = Math.Max(21, _chargeStop);
            ChargeCustomHint.Text = L.T("自定义范围：开始值最低 20%，停止值最高 100%，且开始值必须小于停止值。");
        }
        finally { _chargeSyncing = false; }
    }

    private async void ChargeCustom_Click(object sender, RoutedEventArgs e)
    {
        if (double.IsNaN(ChargeStartBox.Value) || double.IsNaN(ChargeStopBox.Value))
        {
            ChargeCustomHint.Text = L.T("请先填写开始和停止充电百分比。");
            return;
        }
        int start = (int)Math.Round(ChargeStartBox.Value);
        int stop = (int)Math.Round(ChargeStopBox.Value);
        if (start < 20 || stop > 100 || start >= stop)
        {
            ChargeCustomHint.Text = L.T("阈值无效：开始值不能低于 20%，停止值不能高于 100%，并且开始值必须小于停止值。");
            return;
        }
        ChargeCustomHint.Text = L.T("正在写入自定义阈值…");
        await ApplyChargeAsync(start, stop, L.F($"自定义充电阈值已写入：{start}% 开始 / {stop}% 停止。"));
    }

    private async Task ApplyChargeAsync(int start, int stop, string success)
    {
        await RunAsync(async () =>
        {
            try
            {
                string command = start == 0 && stop == 100 ? "charge-off" : $"charge-custom:{start}:{stop}";
                UpdateCharge(await _backend.HardwareAsync(command));
                ChargeCustomHint.Text = L.T("固件回读已确认。");
            }
            catch
            {
                UpdateCharge(await _backend.HardwareAsync("charge-status"));
                throw;
            }
        }, success);
    }
}
