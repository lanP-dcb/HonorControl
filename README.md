# Honor Control

[English](README.en-US.md) · [下载](https://github.com/wzk112/HonorControl/releases) · [安装与迁移](INSTALL.zh-CN.md) · [更新记录](CHANGELOG.md)

面向 **荣耀 X14 锐龙版 2024 R5-7640HS（FRI-H56 / 机型代号 FRI-HXX / 支持 BIOS 1.15）** 的独立 WinUI 3 设备控制工具。无需运行荣耀电脑管家。支持简体中文和 English。

**源码公开，仅限非商业个人使用与研究。禁止商用。** 采用 [Honor Control Personal Research License 1.0](LICENSE)，该限制不属于 OSI 定义的开源许可。非荣耀官方产品。

> 界面截图将在 FRI-H56 实机验证后更新。

## 功能

| 项目 | 当前支持 |
| --- | --- |
| 充电保护 | 家用 40–70%、办公 70–90%、差旅 95–100%、自定义阈值；总开关开启默认家用，关闭恢复充满停止；读取电池、供电状态与固件回读 |
| 性能模式 | 平衡与高能（Turbo X）模式切换；HUNTER 仅作展示、不可选；`荣耀电脑管家` 正在管理性能时提示先退出 |
| 显示与色彩 | 通过 Windows 系统伽马表实现护眼、电子书、色温调节与亮度节律，可恢复原始设置；不提供内屏刷新率设置 |
| 机型识别 | 仅适配荣耀 X14 锐龙版 2024 R5-7640HS（FRI-H56；机型代号 FRI-HXX；支持 BIOS 1.15）；其他机型首次运行弹窗提示，顶部红色横幅不可关闭，全部功能仅作展示 |
| 常驻与界面 | Program Files 安装、登录启动、托盘常驻、后台恢复；Fn/F1–F12 优先级设置；教程、更新记录、关于页及系统动画偏好 |
| 精简范围 | 本版本已移除网络加速、音频、显卡切换、DTT/OS Turbo、自动性能、进程调度、风扇状态读取与风扇预设；HUNTER 仅展示 |

## 安装

在 [Releases](https://github.com/wzk112/HonorControl/releases) 下载 `HonorControl-0.15.4-Setup-x64.exe`，双击并授权一次 UAC。安装器完成文件复制、开始菜单和登录启动注册，无需手动运行 PowerShell；安装完成后不会自动启动，可在结束页勾选“运行 Honor Control”后点击“确定”启动。

本版本不再随安装包携带 .NET 8 运行时。安装器会先检查 **.NET 8 Desktop Runtime（x64）**；如果缺失，会提示并打开微软官方下载页，安装运行时后重新运行安装器即可。

安装到 `C:\Program Files\HonorControl`，注册 `HonorControl.Agent` 和 `HonorControl.UI` 两项登录任务。从开始菜单打开，首次前台打开显示使用说明。关闭窗口后托盘继续运行，右键托盘退出可暂停当前登录会话。升级保留个人设置。

本机型的荣耀电脑管家安装包未提供 FRI/H 系列的原厂 ICC、LUT 和色彩管理配置，因此安装器不会导入工厂色彩数据；显示功能改用 Windows 系统伽马表。公共包不包含原厂 DLL、驱动或校准数据。详见[安装指南](INSTALL.zh-CN.md)。

荣耀电脑管家、独立 OSD、HUNTERCAMP、Magic 视界及 YOYO 等软件后台可卸载。**保留 AMD Radeon 显卡驱动、ACPI/WMI 设备 `ACPI\PNP0C14\HWMI`、HID 与芯片组驱动；不要删除 DriverStore 或 PnP 设备。** 清理脚本默认只列计划，显式传入 `-Apply` 才执行，报告写入本机。

## 支持与限制

- 固件写入仅面向荣耀 X14 锐龙版 2024 R5-7640HS（FRI-H56；机型代号 FRI-HXX；支持 BIOS 1.15）。其他 CPU、BIOS 或机型一律只读，并显示检测到的实际配置。
- 充电保护写入和性能模式写入使用选定 WMI 命令；0.15.4 安装包的 Broker 已启用 `--allow-writes`，但只执行能力位允许的控制，固件保留最终控制权。
- 高能模式需要插电且电量至少 20%。荣耀电脑管家正在管理性能模式时，Honor Control 会提示先退出电脑管家，避免两套后端争抢。
- 显示与色彩仅使用 Windows 系统伽马表，提供护眼、电子书、色温与亮度节律；本机型没有原厂硬件 LUT、ICC 或色彩管理配置，因此不提供原生/sRGB/P3 硬件切换。
- 网络加速、音频、显卡切换、DTT/OS Turbo、自动性能、进程调度、风扇状态读取与风扇预设已从本版本移除，不再加载对应模块；HUNTER 仅展示不可选。

## 构建与数据

Windows x64、.NET 8 SDK，以及 Windows 桌面编译所需组件。

```powershell
.\Build.ps1 -OutputDirectory .\artifacts\HonorControl -WithoutCalibration
```

`App` 为 WinUI 3 界面，`Broker` 提供限定命令范围的高权限控制与后台保活，`Shared` 包含协议、能力档案与运行身份，`BackendSource` 通过系统和已安装驱动接口操作设备。输出包含 .NET 和 Windows App SDK 运行库。本机型没有工厂色彩校准数据，显示使用系统伽马表。

日志、偏好与恢复记录位于 `%LOCALAPPDATA%\HonorControl`。应用不自动上传日志，也不自动检查更新。选择文档链接时会打开浏览器，网络功能按用户操作访问网络。仓库不包含个人设置、SID、注册表备份、恢复备份或 OEM 二进制。

第三方依赖沿用各自许可，见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。商用和组织部署须另行取得书面许可。

构建单文件安装器：

```powershell
.\Build-Installer.ps1 -PackageDirectory .\artifacts\HonorControl
```

在 Windows「已安装的应用」选择 Honor Control 可卸载。卸载保留个人设置、日志和硬件驱动。

运行日志单文件上限 2 MiB，每类最多 3 份；超过 14 天未更新的日志会清理。此规则不删除恢复记录、偏好或校准备份。
