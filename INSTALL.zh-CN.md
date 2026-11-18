# 安装与迁移

Honor Control 0.15.3 适配 Windows x64 的荣耀 X14 锐龙版 2024 R5-7640HS（FRI-H56；机型代号 FRI-HXX；支持 BIOS 1.15）。安装包的 Broker 已启用写入，但只执行本机型能力位允许的充电保护与平衡/高能控制。完整安装目录约占用 95 MB，需保留整个解压目录。

本版本采用框架依赖发布，需要 **.NET 8 Desktop Runtime（x64）**。

## 安装与升级

1. 从 Releases 下载 HonorControl-0.15.3-Setup-x64.exe，双击并授权一次 UAC。安装器会先检查 .NET 8 Desktop Runtime（x64）；缺失时提示并打开微软官方下载页，安装运行时后重新运行安装器。
2. 点击安装，等待完成。安装到 C:\Program Files\HonorControl，注册当前用户的托盘和设备控制登录任务。
3. 安装完成页可按需勾选“运行 Honor Control”，再点击“确定”启动；不勾选则安装器结束后不会自动运行。之后也可从开始菜单打开。首次前台打开显示教程及官方组件迁移清单，登录后台启动不会打断用户。
4. 在关于页切换简体中文 / English，点击重新打开应用语言。升级重复运行新版安装器，保留偏好和日志。
5. 通过 Windows「已安装的应用」中的 Honor Control 卸载。卸载保留个人数据、日志和硬件驱动。

本机型的荣耀电脑管家安装包没有提供 FRI/H 系列的原厂 ICC、LUT 与色彩管理配置，安装器不会导入工厂色彩数据。显示与色彩功能改用 Windows 系统伽马表，可在应用内调节护眼、电子书与色温，并恢复原始设置。

本机型没有可导入的工厂校准数据，也不需要 ICC/LUT 导入；显示调整直接使用 Windows 系统伽马表。

开发者和手动包仍可使用 Install.ps1，普通用户直接运行 Setup。请使用日常登录的管理员账户，不要通过另一个账户的凭据安装。

## 原厂组件与卸载

原厂管家的性能、色彩和 OSD 后台可能与 Honor Control 同时修改设备设置。迁移时先安装并验证 Honor Control，再通过「设置 → 应用 → 已安装的应用」或「控制面板 → 程序和功能」卸载荣耀电脑管家。需要清除原厂提示时，检查发布者为荣耀的 OSD 组件。HUNTERCAMP、Magic 视界、YOYO 助理等按所需功能处理。 [微软卸载说明](https://support.microsoft.com/en-gb/windows/uninstall-or-remove-apps-and-programs-in-windows-4b55f974-2cc6-2d2b-d092-5905080eaf98) · [荣耀卸载说明](https://www.honor.com/cn/support/content/zh-cn15813919/)

卸载荣耀电脑管家后，Honor Control 不依赖 `PCManager.exe`、`MBAMainService`、`HnPerformanceCenter`、`HnPerfPowerNexus`、`HnLcdEnhancement`、`SmartChargePlugin` 或 ICC/LUT 文件；充电保护和平衡/高能走本机 ACPI/WMI 固件接口，显示 gamma 走 Windows 接口。

需要保留以下底层组件。

| 功能 | 依赖 |
| --- | --- |
| 充电保护、平衡/高能 | 本机 ACPI/WMI 设备 `ACPI\PNP0C14\HWMI`（Windows `wmiacpi`）与 BIOS/UEFI 固件 |
| Fn / F1–F12 优先级 | 本机 ACPI/WMI 设备 `ACPI\PNP0C14\HWMI` |
| 护眼、电子书、色温 | Windows 系统伽马表；不需要 `HnLcdEnhancement`、ICC 或硬件 LUT |
| 音频、风扇预设、GPU 切换、DTT | 已从本版本移除，不加载对应模块 |

不要删除 `ACPI\PNP0C14\HWMI` 设备、AMD 显卡驱动、HID 与芯片组驱动；荣耀名称也可能出现在摄像头、音频等设备驱动中，删除会影响对应硬件。

建议卸载顺序：

1. 安装并验证 Honor Control（充电阈值、平衡/高能、Fn、gamma）。
2. 从托盘退出荣耀电脑管家性能后台；确认 `HnPerformanceCenter` 与 `HnPerfPowerNexus` 已结束。`MBAProcessWatcher` 可能重新拉起它们；必要时先停止 `MBAMainService`。
3. 通过 Windows「已安装的应用」卸载荣耀电脑管家，重启。
4. 在设备管理器确认 `ACPI\PNP0C14\HWMI` 仍为正常；运行 `platform-status`、`charge-status`，再测试一次充电写入、平衡/高能切换和 gamma 恢复。
5. 确认无误后，可用随包脚本清理软件残留；默认只输出计划，管理员运行并明确传入 `-Apply` 才执行。

```powershell
.\Clean-HonorSoftware.ps1
.\Clean-HonorSoftware.ps1 -Apply
```

如果卸载后 WMI 返回“拒绝访问”或设备缺失，先重新安装荣耀官网对应机型的 ACPI/BIOS 驱动包，而不要重装整套电脑管家。脚本不删除 DriverStore、PnP 设备、`ProgramData\HonorEffect` 或音频驱动，不自动重启；失败与占用项会写入 `%LOCALAPPDATA%\HonorControl\Migration`。

## 验证

- 重新登录后确认托盘与硬件控制已连接；关于页显示 `FRI-HXX / BIOS 1.15`。
- 读取当前性能模式，应出现平衡与高能；HUNTER 仅展示且不可选。
- 读取充电阈值，显示当前电池百分比与接电状态；测试家用、办公、差旅、自定义以及总开关。
- 显示与色彩页只应提供护眼、电子书与色温；调整 gamma 后点恢复应回到原始状态。
- 网络、音频、GPU、风扇预设与 DTT 已从本版本移除，不再出现在界面或安装包中。

## 暂停与恢复

右键托盘选择退出，可暂停当前登录会话的常驻控制。关闭充电保护或性能模式自动控制时，应用按恢复记录还原自己修改的设置；退出应用会恢复原始 Windows 伽马表。

需要恢复原厂功能时，从荣耀官网按对应机型下载安装包和驱动。重启后检查音频、显示与性能状态，再决定启用哪个控制软件。 [荣耀驱动下载说明](https://www.honor.com/cn/support/content/zh-cn15816112)
