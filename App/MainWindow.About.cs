using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HonorControl;

public sealed partial class MainWindow
{
    internal const string Version = "0.15.0";
    private bool _guideOpen;
    private static string WelcomeRecord => Path.Combine(AppPaths.UserState, "welcome-version.txt");

    private void InitializeAbout()
    {
        AboutVersion.Text = "Honor Control " + Version;
        FooterVersion.Text = "Honor Control " + Version;
        Navigation.OpenPaneLength = L.English ? 250 : 202;
        AboutDataText.Text = L.T("日志与恢复记录保存在本机 LocalAppData\\HonorControl。") + Environment.NewLine +
            L.T("运行日志每份最多 2 MiB，每类最多 3 份，超过 14 天未更新的日志自动清理。偏好、恢复记录与校准备份保留。");
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
            string migration = await Task.Run(ReadMigrationStatus);
            var tabs = new Pivot();
            tabs.Items.Add(GuideTab(L.T("开始使用"), new[]
            {
                (L.T("1 · 安装与启动"), L.T("双击安装程序，授权一次 UAC。安装到 Program Files，登录后托盘常驻。升级保留偏好和显示伽马备份。")),
                (L.T("2 · 与荣耀电脑管家共存"), L.T("充电阈值可直接控制。切换性能模式前，请从托盘退出荣耀电脑管家，确认 HnPerformanceCenter 和 HnPerfPowerNexus 已结束；否则应用会提示冲突并拒绝写入。")),
                (L.T("3 · 充电保护"), L.T("家用、办公、差旅分别写入 40–70%、70–90%、95–100% 的起止阈值。达到停止阈值后不会强制放电，插电时电量可自然下降。")),
                (L.T("4 · 性能模式"), L.T("只提供智能和高能两档；高能要求插电且电量至少 20%。本机不支持 HUNTER 和风扇预设，切换后显示固件回读值。")),
                (L.T("5 · 显示与色彩"), L.T("「显示与色彩」只调整 Windows 系统伽马表，可选护眼、阅读和色温；不写入 ICC 或硬件 LUT，点恢复或退出应用即可还原。")),
                (L.T("6 · 恢复与记录"), L.T("关闭自动控制后按恢复记录还原修改；日志保存在 LocalAppData\\HonorControl，关于页可重看本说明。"))
            }));
            tabs.Items.Add(GuideTab(L.T("官方组件迁移"), new[]
            {
                (L.T("本机检测"), migration),
                (L.T("建议保留"), L.T("建议保留荣耀电脑管家用于驱动与更新，但切换性能模式前需退出其性能后台。不要删除 ACPI、AMD 显卡、音频和 HID 驱动。")),
                (L.T("可选清理"), L.T("如需完全接管，可按安装指南退出或卸载荣耀电脑管家；卸载后其性能后台不会再与 Honor Control 冲突。"))
            }));
            tabs.Items.Add(GuideTab(L.T("更新记录"), new[]
            {
                (Version, L.T("适配荣耀笔记本 X14 锐龙版 2024（FRI-HXX / M1030 / BIOS 1.15 / 7640HS / Radeon 760M）：充电保护、智能/高能模式、Windows 伽马显示；隐藏网络、音频、GPU、Fn、风扇预设与 DTT。"))
            }));
            tabs.Items.Add(GuideTab(L.T("支持与须知"), new[]
            {
                (L.T("已验证设备"), L.T("实机验证为 FRI-HXX / M1030 / BIOS 1.15 / Ryzen 5 7640HS / Radeon 760M。其他 FRI 配置按功能判断，未承诺整机完整兼容。")),
                (L.T("功能范围"), L.T("充电保护、智能/高能模式、刷新率与 Windows 伽马显示。网络加速、音频、GPU 切换、Fn 行、风扇预设、HUNTER 与 DTT 不启用。")),
                (L.T("已知限制"), L.T("性能模式写入需先退出荣耀电脑管家；色彩不提供 ICC/LUT；HDR、外接屏与多屏未验证。")),
                (L.T("许可与记录"), L.T("源码仅供非商业个人研究使用。设备操作与恢复记录保存在本机，不上传日志。关于页可查看源码、许可、安装指南和更新记录。"))
            }));
            tabs.SelectedIndex = changes || (!first && !force) ? 2 : 0;
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

    private static string ReadMigrationStatus()
    {
        try
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MBAMainService", "GuardService", "HnUpdateService", "OSDServer", "HunterCampService", "MagicAnimationService", "HnPCAIService", "HnPCInferEngine", "QAXDefender.SDK" };
            using var query = new System.Management.ManagementObjectSearcher("SELECT Name,PathName FROM Win32_Service");
            using var result = query.Get();
            var found = result.Cast<System.Management.ManagementObject>().Where(item =>
                names.Contains(Convert.ToString(item["Name"]) ?? "") &&
                ((Convert.ToString(item["PathName"]) ?? "").Contains("\\HONOR\\", StringComparison.OrdinalIgnoreCase) ||
                 (Convert.ToString(item["PathName"]) ?? "").Contains("\\RPC\\OSD\\", StringComparison.OrdinalIgnoreCase)))
                .Select(item => Convert.ToString(item["Name"])).ToArray();
            return found.Length == 0 ? L.T("上述荣耀软件服务均未检测到。下方仍列出具体名称，便于迁移其他设备时核对。")
                : L.T("检测到的软件服务：") + string.Join(", ", found);
        }
        catch { return L.T("无法读取服务清单，请按下方名称在 Windows 服务和已安装的应用中核对。"); }
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
