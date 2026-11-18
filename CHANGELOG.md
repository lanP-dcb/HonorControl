# Changelog / 更新记录

## 0.15.0

- 适配荣耀笔记本 X14 锐龙版 2024（FRI-H56 / FRI-HXX / M1030 / BIOS 1.15 / Ryzen 5 7640HS / Radeon 760M）：充电保护、智能/高能性能模式与系统伽马显示；网络、音频、显卡、DTT、风扇状态与预设按能力隐藏。
- 充电保护新增自定义阈值、总开关（开启默认家用，关闭恢复充满停止）与固件回读；Fn/F1–F12 优先级设置启用；HUNTER 仅展示不可选；移除内屏刷新率设置；非适配机型首次运行提示、顶部红色横幅与仅展示状态。
- 精简应用与安装包：移除音频、网络、显卡、DTT、自动性能、进程调度、硬件色彩/ICC/LUT、刷新率、风扇状态读取与风扇写入模块；安装包不再携带色彩校准组件、场景表、App SDK AI/ML/Widgets 运行库与多余语言目录，体积由约 288 MB 降至约 236 MB。
- 覆盖安装时会删除旧版本残留文件，避免升级后仍保留已移除的组件。
- 显示页恢复“亮度节律（系统伽马）”，并移除“夜间模式 / 亮度与显示 / 高级显示设置”三个系统设置跳转按钮。
- 性能与散热页移除风扇卡片，不再读取风扇传感器或显示转速。
- Adapts the HONOR MagicBook X14 2024 (FRI-H56 / FRI-HXX / M1030 / BIOS 1.15 / Ryzen 5 7640HS / Radeon 760M): charging protection, Smart/Performance modes and system gamma display; network, audio, GPU, DTT, fan status and presets are capability-hidden.
- Adds custom charge thresholds, a master switch (Home when on, charge-to-full when off) and firmware readback; enables Fn / F1–F12 priority; shows HUNTER as display-only; removes the refresh-rate setting; and adds first-run unsupported-device notice, a non-closable red banner and display-only controls.
- Simplifies the app and installer: removes audio, network, GPU, DTT, automatic performance, process scheduling, hardware colour/ICC/LUT, refresh-rate, fan-status reading and fan-write modules; the installer no longer ships colour calibration components, scene tables, App SDK AI/ML/Widgets runtimes or extra locale folders, reducing the payload from about 288 MB to about 236 MB.
- Upgrades now remove files left by older versions so deleted components do not remain in the installation directory.
- Restores brightness rhythm (system gamma) on the display page and removes the Night light / Brightness and display / Advanced display settings shortcuts.
- Removes the fan card from the Performance and cooling page; fan sensors are no longer read or displayed.

## 0.14.5

- 安装器自动检查 DRA-XX / TMA2024 的本机原厂 ICC 与 sRGB、P3 校准 XML，导入结果显示在安装完成页。
- Setup detects local factory ICC profiles and sRGB/P3 calibration XML on DRA-XX / TMA2024 and reports the import result on completion.

## 0.14.4

- 安装与卸载窗口适配高 DPI 和较小屏幕。说明可滚动，底部按钮随窗口排版。
- Setup and uninstall windows adapt to high DPI and smaller screens, with scrollable instructions and a layout that keeps the action buttons visible.

## 0.14.3

- Runtime logs rotate at 2 MiB with three files per stream. Logs not updated for 14 days are removed. Recovery records and calibration backups are retained.
- 日志自动滚动并清理旧文件，恢复记录与校准备份保留。

## 0.14.2

- Single-file bilingual setup and uninstaller; preserves preferences and local calibration, registers sign-in startup.
- Specific OEM applications / service names and live service detection in the migration tutorial.
- Handles Fn+P denied-power event 0x2A6, including battery use; centers warning text; fixes the color-reapply allowlist after refresh-rate changes.
- 新增双语单文件安装与卸载程序、具体原厂服务迁移清单，修复拔电 Fn+P 提示与浮窗文字对齐。

## 0.14.1

- Centered the tutorial using the internal dialog width resource; improved theme contrast and expanded usage steps.
- Documented per-feature configuration support; limited the verified DTT policy table to 125H.
- Kept network profile names and identifiers unchanged when translating status messages.
- 修复教程居中与说明文字对比度，增加各功能使用步骤和同型号不同配置的支持说明。

## 0.14.0

- First-launch tutorial, update notes and About page; foreground opens only.
- Chinese and English UI, including settings, status, tray and notifications.
- Slower native page transitions: 320 ms fade and 380 ms translation, following Windows animation preferences.
- Twelve additional game process rules including CS2, League of Legends, PUBG, CrossFire, Genshin Impact and Star Rail. Excludes launchers and common helper processes from automatic game sessions.
- Installation and migration guides; auditable OEM software cleanup script; public builds omit device calibration data and support local import.

- 新增首次启动教程、更新记录、关于页及中英文界面。
- 页面动画放慢，保留 Windows 关闭动画时的即时切换。
- 补充常见游戏主进程，排除启动器和辅助进程误触发。
- 提供安装迁移指南、原厂软件清理脚本与本机校准导入工具。

## 0.13.7

- Histen speaker / headphone settings and Fortemedia single-person, group and ambient microphone pickup modes.
- Function-specific OSD icons and reasons for denied performance changes.
- Read-back HUNTER fan levels; rejected manual changes preserve automatic mode.
- Automatic performance keeps a recognized running game across focus changes, with a 45-second process-gap grace period.
- Fixed nullable JSON status parsing and added WinUI navigation transitions.
