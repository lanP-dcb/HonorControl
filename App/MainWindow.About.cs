using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HonorControl;

public sealed partial class MainWindow
{
    internal const string Version = "1.0.0";
    private bool _guideOpen;
    private static string WelcomeRecord => Path.Combine(AppPaths.UserState, "welcome-version.txt");
    private static string UnsupportedRecord => Path.Combine(AppPaths.UserState, "unsupported-version.txt");

    private void InitializeAbout()
    {
        AboutVersion.Text = "Honor Control " + Version;
        FooterVersion.Text = "Honor Control " + Version;
        Navigation.OpenPaneLength = L.English ? 250 : 202;
        AboutDataText.Text = L.T("日志与显示复原记录保存在本机 LocalAppData\\HonorControl。") + Environment.NewLine +
            L.T("运行日志每份最多 2 MiB，每类最多 3 份，超过 14 天未更新的日志自动清理。偏好与显示伽马备份保留。");
        LanguagePicker.SelectedIndex = L.English ? 1 : 0;
    }

    private async Task ShowStartupGuideAsync(bool force = false, bool changes = false)
    {
        if (!_ready || _closing || _guideOpen || Root.XamlRoot is null) return;
        bool first = !File.Exists(WelcomeRecord);
        if (!force && !first && File.ReadAllText(WelcomeRecord).Trim() == Version) return;
        _guideOpen = true;
        try
        {
            var tabs = new Pivot();
            tabs.Items.Add(GuideTab(L.T("开始使用"), new[]
            {
                (L.T("1 · 安装与启动"), L.T("双击安装程序，授权一次 UAC。安装到 Program Files，登录后托盘常驻。升级保留偏好和显示伽马备份。")),
                (L.T("2 · 充电保护"), L.T("家用、办公、差旅分别写入 40–70%、70–90%、95–100% 的起止阈值；自定义模式可设置开始 20–99%、停止 21–100%，且开始值必须小于停止值。总开关开启时默认家用，关闭时恢复 0–100% 充满停止。达到停止阈值后不会强制放电，插电时电量可自然下降。")),
                (L.T("3 · 性能模式"), L.T("提供平衡和高能两档；高能要求插电且电量至少 20%。HUNTER 仅作展示、本机不可选。切换后显示固件回读值。")),
                (L.T("4 · 显示与色彩"), L.T("「显示与色彩」只调整 Windows 系统伽马表，可选护眼、阅读和色温；不写入 ICC 或硬件 LUT，点恢复或退出应用即可还原。")),
                (L.T("5 · Fn 设置"), L.T("可在设置页切换键帽图标功能优先或标准 F1–F12 优先，写入后会从固件读回确认。")),
                (L.T("6 · 记录"), L.T("日志保存在 LocalAppData\\HonorControl，关于页可重看本说明。"))
            }));
            tabs.Items.Add(GuideTab(L.T("更新记录"), new[]
            {
                (Version, L.T("适配荣耀 X14 锐龙版 2024 R5-7640HS（FRI-H56；机型代号 FRI-HXX；支持 BIOS 1.15）：充电保护、平衡/高能模式、Fn 与 F1–F12、Windows 伽马显示；移除未使用的网络、音频、GPU、风扇状态与预设及 DTT 组件。"))
            }));
            tabs.Items.Add(GuideTab(L.T("支持与须知"), new[]
            {
                (L.T("已验证设备"), L.T("实机验证为荣耀 X14 锐龙版 2024 R5-7640HS（FRI-H56；机型代号 FRI-HXX；支持 BIOS 1.15）。其他 FRI 配置按功能判断，未承诺整机完整兼容。")),
                (L.T("功能范围"), L.T("充电保护、平衡/高能模式、Fn 与 F1–F12 默认行为、Windows 伽马显示。HUNTER 仅展示。")),
                (L.T("已知限制"), L.T("性能模式写入需先退出荣耀电脑管家；色彩不提供 ICC/LUT；HDR、外接屏与多屏未验证。")),
                (L.T("许可与记录"), L.T("源码仅供非商业个人研究使用。设备操作与恢复记录保存在本机，不上传日志。关于页可查看源码、许可、安装指南和更新记录。"))
            }));
            tabs.SelectedIndex = changes || (!first && !force) ? 1 : 0;
            var dialog = new ContentDialog
            {
                XamlRoot = Root.XamlRoot,
                RequestedTheme = Root.RequestedTheme,
                Title = L.T("Honor Control · 使用说明"),
                Content = tabs,
                CloseButtonText = L.T("知道了"),
                DefaultButton = ContentDialogButton.Close
            };
            dialog.Resources["ContentDialogMaxWidth"] = 720d;
            await dialog.ShowAsync();
            Directory.CreateDirectory(AppPaths.UserState);
            File.WriteAllText(WelcomeRecord, Version);
        }
        catch (Exception ex) { Log(Friendly(ex)); }
        finally { _guideOpen = false; }
    }

    private async Task ShowUnsupportedNoticeAsync()
    {
        if (!_ready || _closing || Root.XamlRoot is null) return;
        try
        {
            if (File.Exists(UnsupportedRecord) && File.ReadAllText(UnsupportedRecord).Trim() == Version) return;
        }
        catch { }
        try
        {
            var dialog = new ContentDialog
            {
                XamlRoot = Root.XamlRoot,
                RequestedTheme = Root.RequestedTheme,
                Title = L.T("设备未通过适配检查"),
                Content = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = L.T("当前设备不在 Honor Control 的适配范围内。软件可以打开，但所有功能仅作展示，不会读取或写入设备。") + Environment.NewLine + Environment.NewLine +
                        L.T("支持的机型：荣耀 X14 锐龙版 2024 R5-7640HS（FRI-H56；机型代号 FRI-HXX；支持 BIOS 1.15）。")
                },
                CloseButtonText = L.T("我知道了"),
                DefaultButton = ContentDialogButton.Close
            };
            dialog.Resources["ContentDialogMaxWidth"] = 560d;
            await dialog.ShowAsync();
            Directory.CreateDirectory(AppPaths.UserState);
            File.WriteAllText(UnsupportedRecord, Version);
        }
        catch (Exception ex) { Log(Friendly(ex)); }
    }

    private static PivotItem GuideTab(string title, IEnumerable<(string Title, string Text)> sections)
    {
        var panel = new StackPanel { Spacing = 20, Margin = new Thickness(0, 12, 16, 12), MaxWidth = 600 };
        foreach (var section in sections)
        {
            var block = new StackPanel { Spacing = 6 };
            block.Children.Add(new TextBlock { Text = L.T(section.Title), FontSize = 17, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            block.Children.Add(new TextBlock { Text = L.T(section.Text), Style = (Style)Application.Current.Resources["CaptionLabel"], FontSize = 14, TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(block);
        }
        return new PivotItem { Header = L.T(title), Content = new ScrollViewer { Content = panel, MaxHeight = 470, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } };
    }

    private async void Guide_Click(object sender, RoutedEventArgs e) => await ShowStartupGuideAsync(force: true);
    private async void Changes_Click(object sender, RoutedEventArgs e) => await ShowStartupGuideAsync(force: true, changes: true);
    private void Repository_Click(object sender, RoutedEventArgs e) => OpenDocument("https://github.com/wzk112/HonorControl");
    private void License_Click(object sender, RoutedEventArgs e) => OpenDocument("https://github.com/wzk112/HonorControl/blob/main/LICENSE");
    private void InstallGuide_Click(object sender, RoutedEventArgs e) => OpenDocument("https://github.com/wzk112/HonorControl/blob/main/INSTALL." + (L.English ? "en-US" : "zh-CN") + ".md");

    private void OpenDocument(string address)
    {
        try { Process.Start(new ProcessStartInfo(address) { UseShellExecute = true }); }
        catch (Exception ex) { Show(Friendly(ex), InfoBarSeverity.Error); }
    }

    private void Language_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready) return;
        string choice = LanguagePicker.SelectedIndex == 1 ? "en-US" : "zh-CN";
        Directory.CreateDirectory(AppPaths.UserState);
        File.WriteAllText(Path.Combine(AppPaths.UserState, "language.txt"), choice);
        RestartLanguageButton.Visibility = choice == L.Language ? Visibility.Collapsed : Visibility.Visible;
    }

    private void RestartLanguage_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || _guideOpen) return;
        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, ArgumentList = { "--restart" } });
            _quitRequested = true;
            Close();
            Application.Current.Exit();
        }
        catch (Exception ex) { Show(Friendly(ex), InfoBarSeverity.Error); }
    }
}
