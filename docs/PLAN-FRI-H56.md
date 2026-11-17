# Honor Control — FRI-H56 适配计划

> 本计划取代原先的 HONOR MagicBook Pro 16 DRA-XX / BIOS 1.13 适配计划。
> 目标机型只有一个：**荣耀笔记本 X14 锐龙版 2024 R5 集显 16GB+512GB（零售名 FRI-H56）**。
> DRA-XX 的兼容验证不再作为目标，不为其继续投入实现或回归成本。

## 0. 范围、优先级与非目标

### 0.1 目标机型

| 项目 | 本机实测值 |
| --- | --- |
| 产品名 / 零售名 | `FRI-HXX` / FRI-H56 |
| SystemVersion | `M1030` |
| 主板 | `FRI-HXX-PCB`，Version `M1030` |
| BIOS | `1.15`（2025-03-05） |
| CPU | AMD Ryzen 5 7640HS（6C/12T，Radeon 760M） |
| GPU | AMD Radeon 760M（`PCI\VEN_1002&DEV_15BF...`） |
| 内存 / 系统 | 16 GB / Windows 11 家庭中文版 10.0.26100 |
| 面板 | CSOT `CSO1424`（`DISPLAY\CSO1424\5&d5b025&0&UID256_0`） |

`PlatformProfile.Detect()` 只把 **Model=FRI-HXX + SystemVersion=M1030 + BIOS=1.15 + CPU 含 7640HS + GPU 含 Radeon 760M** 识别为支持配置；其余一律 `Unsupported`，`AssertWrites()` 保持 fail-closed。`SystemVersion` 来自 `Win32_ComputerSystemProduct.Version`，用于与同平台其他 FRI-HXX 配置区分。

### 0.2 功能优先级

1. **P1 充电保护**：读取/设置充电起止阈值，显示当前充电状态。
2. **P2 电源管理（HONOR Turbo X）**：智能模式 / 高能模式切换；只做 0/1 两种 WMI 模式。
3. **P3 显示与色彩**：Windows gamma 级的护眼、阅读、色温与刷新率；不做原厂硬件 LUT/ICC。

### 0.3 明确不做 / 直接隐藏

- 网络加速：不实现、不接线，整页隐藏；`network-configure` 仍由 Broker 拒绝。
- HUNTER 模式、五档风扇预设：本机无圆口电源、无对应风扇预设，隐藏。
- 独显/双显卡切换：本机只有 Radeon 760M 集显，隐藏 GPU 卡片。
- DTT / OS Turbo 场景调度、EPP、PL1：AMD 平台无可用 DTT 语义，隐藏自动性能与场景功耗开关。
- 音频 Histen/Fortemedia：本机包内无对应适配，隐藏音频页。
- 原厂色彩管理、sRGB/P3/Native 硬件 LUT、ICC 校准、ColorProfileCard：FermiH 的 PC Manager 包没有 `ColorManagement`、`hngamut.zip`、`Icc`，且现有 Intel IGCL 路径不适用于 AMD；隐藏，不用软件伪造“原厂校准”。
- Fn 默认行为写入与麦克风灯：保持只读/隐藏，不纳入本轮。
- 安装器不再宣传 DRA-XX / TMA2024 的 ICC 自动导入。

## 1. 重新检查结论（2026-11-17）

### 1.1 软件版本

| 组件 | 版本 |
| --- | --- |
| 荣耀电脑管家 PCManager | `20.0.0.50(SP5C233)`（本次会话中从 20.0.0.45 自动更新） |
| BasicService | `20.0.0.60(C233)` |
| PerformanceCenter | `20.0.0.261(SP2C233)` |
| LCDEnhancement | `20.0.0.50(SP5C233)` |
| Honor Control | 当前仓库 `0.14.5` |

更新电脑管家后重新扫描 `C:\Program Files\HONOR\HnLcdEnhancement\config`：`FermiH_FermiHB_BornH_BornHB_FermiI_BornI` 的 `BasicDisplay.xml` 仍只有 `EyeProtect`、`NatualLight`、`ColorTemperatureAdjust`，没有 `<EbookMode>`、`<ColorManagement>`、`<ColorSpace>`；`hngamut.zip` 与 `Icc` 目录仍没有 FermiH。结论：**更新电脑管家不能补齐本机原厂色彩管理/ICC**，显示适配只能走 Windows gamma。

**组件完整性复核（2026-11-17 22:05）：** 再次扫描 `C:\Program Files\HONOR`，核心组件版本与上表一致；充电相关 `C:\Program Files\HONOR\PCManager\BatteryStateMonitor.dll`、`C:\Program Files\HONOR\PCManager\plugins\SmartChargePlugin.dll`、`C:\Program Files\HONOR\PCManager\ChargeTurboPopUp.exe` 均在；性能相关 `HnPerformanceCenter.exe`、`HnPerfPowerNexus.exe` 均在运行；显示相关 `HnLcdEnhancement` 文件与 FermiH 配置完整。缺失的目录为 `HNPCAIService`、`HNPCInferEngine`、`MagicClaw`；它们属于 YOYO/AI/OCR 辅助组件，不影响本计划的三项功能。若需要电脑管家 AI/YOYO 功能，需修复安装。

### 1.2 能力与证据矩阵

| 能力 | 读取 | 写入 | 本机证据 | 结论 |
| --- | --- | --- | --- | --- |
| 充电保护 | WMI `3/17` GBTT，bytes 1/2 = start/stop | WMI `3/16` SBTT | 电池 70%，`PowerOnline=True`、`Charging=False`；`HKLM\SOFTWARE\PCManager\MBAPowerManager\PowerSafeManagerStatus=1`、`PowerSafeManagerMode=0` | P1 可做；先读 3/17 确认阈值 |
| Turbo X 智能/高能 | WMI `4/14` GTUB，字节 `[1]` 为模式码（实测 0） | WMI `4/15` STUB | `HnPerformanceCenter.exe`、`HnPerfPowerNexus.exe` 运行；`HKLM\SOFTWARE\HONOR\PCManager\CurrentPowerMode=1`；BasicService/PCManager 均有 Turbo 设置布局 | P2 可做，但要处理与电脑管家的控制冲突 |
| 显示/色彩 | 配置只读；Windows 显示器 API | 无原厂 LUT | FermiH `BasicDisplay.xml` 仅 EyeProtect/NatualLight/ColorTemperatureAdjust；无 `ColorManagement`/`EbookMode`/`hngamut`/`Icc`；Intel IGCL 要求 `VEN_8086` | P3 只做 OS gamma；不碰原厂校准 |
| 风扇 | WMI `2/8` 实际转速；`2/2` 温度 | 无 | 实测 2481–2829 RPM，50–57°C；无圆口电源；无 `7/8`、`7/13` | 只读显示，隐藏风扇预设/HUNTER |
| GPU 切换 | 无 | 无 | 只有 Radeon 760M；`6/42`、`6/43`、`6/41` 不存在 | 隐藏 GPU 功能 |
| DTT / OS Turbo | 无 | 无 | `Dtt=false`；`3/14`、`3/15` 是 ODP 语义，不适用；`HnPerformanceCenter` 存在时 `DttPolicy` 还会主动阻断 | 隐藏自动性能与场景功耗 |
| 网络加速 | — | — | 与当前机型无关，用户明确可不做 | 整页隐藏，Broker 拒绝 |

### 1.3 WMI/ACPI 事实

- WMI 设备：`ACPI\PNP0C14\HWMI_0`（另有 `HWMI_1`，不使用）；`OemWMIMethod.OemWMIfun` 64 字节 `u8Input`，首字节 `data[0]==0` 为成功。
- 关键方法（来自 `.probe\acpi\dispatch-table.txt`）：
  - 充电读 `GBTT 3/17`，充电写 `SBTT 3/16`。
  - 模式读 `GTUB 4/14`，模式写 `STUB 4/15`。
  - 风扇转速 `2/8`，温度 `2/2`。
  - 风扇预设读 `GFNM 4/12`、写 `SFNM 4/13`；本机**没有** `7/8`、`7/13`。现有 `HonorFirmware.SetMode()` 在 HUNTER 时写 `Call(7,8,...)`，对 FRI 是错误路径，HUNTER 必须禁用。
- 只读实测：`4/14` 返回模式 0（智能）；`2/2` 返回 50–51°C；`2/8` 返回 2481 RPM；`2/9` 参数 2 返回 20000 mV。
- 写入尚未实测；当前 shell `IsAdmin=False`，直接读 `OemWMIMethod` 会被拒绝。所有写测试必须通过提升后的 Broker 或管理员脚本执行。

### 1.4 充电现状

当前电池 `EstimatedChargeRemaining=70`、`BatteryStatus=2`（交流供电、未充电），`DesignVoltage=12136 mV`，活动电源方案为“平衡”。这与 `PowerSafeManagerStatus=1` 的充电保护启用状态一致；实际 start/stop 仍需通过 `3/17` 读取确认后再写入。

## 2. 目标能力矩阵

| 功能 | 读取 | 写入 | UI | 优先级 |
| --- | --- | --- | --- | --- |
| 充电状态 | `charge-status`（3/17） | — | 显示当前百分比、是否接电、保护阈值 | P1 |
| 充电保护预设 | 同上 | `charge-home` 40/70、`charge-office` 70/90、`charge-travel` 95/100 | 三个按钮 + 当前选择 + 读回 | P1 |
| Turbo X 智能/高能 | `firmware-status`（4/14） | `firmware-smart`（0）、`firmware-performance`（1） | 只显示两个按钮 + 当前模式 + 供电条件提示 | P2 |
| 显示刷新率 | Windows 显示 API | Windows 显示 API | 保留刷新率选择 | P3 |
| 护眼/阅读 | 本地设置 | Windows gamma ramp | 两个模式开关 | P3 |
| 色温 | 本地设置 | Windows gamma ramp | −100..100 滑杆 | P3 |
| 原厂色彩 LUT/ICC | — | — | 隐藏 | — |
| 网络加速 | — | — | 整页隐藏 | — |
| 风扇/HUNTER/GPU/音频/DTT/Fn 写入 | 只读或 — | — | 隐藏或只读 | — |

## 3. 设计决策

### 3.1 平台识别与能力开关

- 修改 `BackendSource/PlatformProfile.cs`：
  - 删除/停用 DRA-XX 支持分支；只保留 FRI-H56 分支（`FRI-HXX` + `M1030` + `1.15` + `7640HS` + `Radeon 760M`）。
  - FRI 分支设置：`FirmwareWrites=true`、`ModeWrite=true`、`ChargeWrite=true`；其余 `FnWrite/MicLight/ColorLut/Audio/Hunter/FanPreset/DiscreteGpu/Dtt/SecondFan` 保持 `false`。
  - 新增 `DisplayGamma=true`、`Network=false`（或 `NetworkAcceleration`），并在 `Snapshot()` 中输出。
  - `WriteBlockReason` 对 Unsupported 机型保留实际检测值，便于排错。
- `AssertWrites()` 仍要求 `FirmwareWrites`，但每个命令必须再检查自己的能力位；不能因为 `FirmwareWrites=true` 就允许风扇/HUNTER 写入。

### 3.2 充电保护

- 复用现有 `ChargeSnapshot()` 与 `SetCharge(start, stop)`；写入前先读 `3/17`，写后按 `3/17` 回读确认，失败恢复原值。
- 新增 `charge-status` 返回：`ChargeStart`、`ChargeStop`、`AcOnline`、`BatteryPercent`、`ChargeSupported`、`StatusText`。
- 三个预设沿用现有命令；不新增 WMI 方法。
- UI 文案明确“保护阈值由固件执行”，并显示当前是否在保护；未接电时允许设置阈值但提示“接入电源后生效”。
- 持久化：保存用户最后一次选择的预设到 `%LOCALAPPDATA%\HonorControl\charge-preference.json`，启动时以 `3/17` 读回值为准，不盲信本地文件。

### 3.3 Turbo X 智能/高能

- `HonorFirmware.SetMode(target)` 在 FRI 只接受 `0`、`1`；收到 `3` 返回 `HUNTER mode is not available on this platform.`，且绝不调用 `Call(7,8,...)`。
- `firmware-smart`、`firmware-performance` 走现有 `Broker/PerformanceController.ApplyManualAsync`；`firmware-hunter` 在 Broker 层按平台能力拒绝。
- `Snapshot()` 输出 `ModeCode`、`ModeName`、`ModeWrite`；UI 只渲染“智能模式/高能模式”两个按钮，隐藏 HUNTER 与风扇预设。
- **与电脑管家的冲突处理**：`HnPerformanceCenter.exe` / `HnPerfPowerNexus.exe` 运行时，PC Manager 是性能控制的实际主人，Honor Control 不强行杀进程。`ApplyManualAsync` 先检测这两个进程，若存在则返回可读原因：“荣耀电脑管家正在管理性能模式，请先退出电脑管家后重试”。UI 在模式卡片显示该提示，并提供“如何退出电脑管家”的说明链接；只有电脑管家退出后才执行 `4/15` 写入。
- 高能模式仍受现有 `CheckPower()` 约束：必须接电且电量 ≥20%。不满足时返回原因，不写入。
- 模式写入后 10×250 ms 回读 `4/14`，若未生效则恢复原模式并报错。

### 3.4 显示与色彩（最低优先级）

- 不修改 `WindowsColor.RequireIntelOutput()` 的语义，也不让 FRI 走 `ControlLib.dll`；新增 `DisplayGamma` 后端能力，独立于 `ColorLut`。
- 复用 `BackendSource/DisplayEffects.cs` 的 `DisplayEffects(style, temperature)` 计算思路，但把结果应用到 Windows gamma ramp（`SetDeviceGammaRamp` / `GetDeviceGammaRamp`）而不是 Intel 3D LUT。
- 支持范围：
  - 刷新率：调用现有 Windows 显示 API；若 AMD 驱动不支持切换则只显示当前值，不伪造成功。
  - 护眼/阅读：两种 gamma 预设，支持恢复。
  - 色温：−100..100，实时预览 + 恢复。
  - 亮度周期：本轮不做，保持隐藏。
- UI 保留精简 DisplayPage：刷新率、护眼、阅读、色温；隐藏 Native/sRGB/P3 按钮、DisplayEffect 外的高级选项、ColorProfileCard、ColorWatch。
- 任何 gamma 写入必须可恢复；应用退出/页面关闭时必须恢复原 gamma；失败要报原因，不得静默。

### 3.5 UI 能力折叠

- 扩展 `App/MainWindow.Platform.cs` 的 `_cap*`：
  - 现有：`_capHunter`、`_capFan`、`_capModeWrite`、`_capDiscrete`、`_capChargeWrite`、`_capColor`、`_capAudio`。
  - 新增：`_capDisplayGamma`、`_capNetwork`（以及如需 `_capFn`）。
- `ApplyPlatformCapabilities()` 中：
  - `NetworkPage` 与对应导航项：`_capNetwork=false` 时隐藏。
  - 自动性能卡片、场景功耗开关、风扇卡片、GPU 卡片、音频页、HUNTER 按钮：FRI 隐藏。
  - Fn 卡片、麦克风灯、键盘写入控件：FRI 隐藏或禁用。
  - DisplayPage：`_capColor=false` 且 `_capDisplayGamma=true` 时显示精简版；两者都 false 时整页隐藏。
  - 充电卡片：`_capChargeWrite=true` 时启用。
  - 模式按钮：`_capModeWrite=true` 时启用；HUNTER 仍按 `_capHunter=false` 隐藏。
- `AboutScopeText` 改为 FRI-H56 文案：`当前机型为 HONOR MagicBook X14 2024 (FRI-H56)：已适配充电保护与 Turbo X 智能/高能模式；显示为 Windows gamma 基础适配。`

### 3.6 Broker 与安装器

- `Broker/Program.cs` 的 `allowed` 集合保留 `charge-*`、`firmware-status/mode/policy/smart/performance`；`firmware-hunter`、`fan-*`、`gpu-*`、`dtt-*`、`audio-*`、`network-configure` 在 FRI 运行时按能力拒绝。
- `IsWrite` 继续把 `charge-*`、`firmware-smart/performance` 视为写操作。
- `Install.ps1` 的 `HonorControl.Agent` 启动参数加入 `--allow-writes` **仅在完成 P1/P2 写入验证并取得用户明确授权后**；在此之前保持不传，默认只读。
- 不修改 `Build.ps1` 的构建流程，不绕过 csc 编译 BackendSource。

### 3.7 文档与版本

- `README.md` / `README.en-US.md`：目标机型改为 FRI-H56；能力表删除 HUNTER、GPU、音频、网络、硬件 LUT 的“支持”描述；加入“电脑管家需退出”的 Turbo X 前置条件。
- INSTALL.zh-CN / INSTALL.en-US：删除 DRA-XX ICC 自动导入说明；补充 FRI 机型只读/写入验证说明。
- CHANGELOG：新版本一条中文 + 一条英文。
- 版本号统一提升到 `0.15.0`（目标机型与能力范围变化），全仓库同步修改；不要只改 About。
- 截图更新 `docs/screenshots/performance.zh-CN.png` 等，反映 FRI 精简界面。

## 4. 实施路线与 TODO

### P0 基线检查与安全验证（先做，不改产品行为）

- [ ] P0-1 用管理员权限运行 `.probe\wmi-read-probe.ps1` 或提升后的 `HonorStandalone.exe`，记录 `4/14`、`3/17`、`2/8`、`2/2`、`2/9` 的原始十六进制与解析值。
- [ ] P0-2 读取当前充电 start/stop（`3/17` bytes 1/2），确认“70% 不充电”对应阈值；记录 `PowerSafeManagerStatus`、`PowerSafeManagerMode`。
- [ ] P0-3 在用户明确授权、接电且电量 ≥20% 的前提下，做一次 `charge-home 40/70` 写入 → `3/17` 回读 → 恢复原值。
- [ ] P0-4 关闭荣耀电脑管家性能组件后，做一次 `firmware-performance` 写入 → `4/14` 回读 → 恢复智能模式；若电脑管家自动恢复，记录现象。
- [ ] P0-5 输出基线表（模式、温度、风扇、充电、阈值的解析值），作为后续回归对照。

**P0 通过标准：** 所有写操作可回读、可恢复；无蓝屏/掉电/风扇异常；若某写入不可用，明确标记对应功能为“阻塞”，不进入实现。

### P1 平台识别与能力折叠

- [ ] P1-1 修改 `BackendSource/PlatformProfile.cs` 的 FRI 分支能力位与 `Detect()`，移除 DRA-XX 受支持分支。
- [ ] P1-2 在 `Snapshot()` 输出新增 `DisplayGamma`、`Network` 能力；同步 `App/Services/BackendClient.cs` 的 `PlatformStatusAsync` 解析。
- [ ] P1-3 修改 `App/MainWindow.Platform.cs`：新增 `_capDisplayGamma`、`_capNetwork`，调整 `ApplyPlatformCapabilities()`。
- [ ] P1-4 修改 `App/MainWindow.xaml`：为 NetworkPage/导航项、Fn 卡片、自动性能卡片等补 `x:Name`，按能力隐藏；精简 DisplayPage。
- [ ] P1-5 `Broker/Program.cs` 加 FRI 能力拒绝；`Install.ps1` 暂不启用 `--allow-writes`。
- [ ] P1-6 在 `Tests/PolicyChecks/Program.cs` 增加 FRI 能力矩阵断言（若 `Detect()` 不易注入，抽一个纯函数 `ForIdentity(...)` 供测试）。

**P1 通过标准：** 本机启动只显示充电/模式/精简显示；网络、风扇、HUNTER、GPU、音频、DTT 不可见；所有隐藏功能的命令即使被手工调用也被 Broker 拒绝；构建通过。

### P2 充电保护（P1 功能优先级）

- [ ] P2-1 实现/补强 `ChargeSnapshot` 返回 `AcOnline`、`BatteryPercent`、`ChargeStart`、`ChargeStop`、`ChargeSupported`。
- [ ] P2-2 `SetCharge` 写入前校验电源/电量；写入后 `3/17` 回读 8×100 ms；失败恢复原值并返回原因。
- [ ] P2-3 `Broker/Program.cs` 允许 `charge-status/home/office/travel`；`IsWrite` 标记写操作；错误信息中文化。
- [ ] P2-4 `App/MainWindow.xaml.cs` 的 `UpdateCharge` 显示当前百分比/接电/阈值；三个按钮只在 `_capChargeWrite` 时启用。
- [ ] P2-5 保存最后选择到 `charge-preference.json`；启动时以 `3/17` 读回值为准。
- [ ] P2-6 手工验证 40/70、70/90、95/100 三组写入/回读/恢复；检查电脑管家是否覆盖。

**P2 通过标准：** 本机可读当前阈值；三组预设均可写入并回读；失败会恢复且提示；电脑重启后 UI 显示与固件一致；无写入时不会误触发。

### P3 Turbo X 智能/高能（P2 功能优先级）

- [ ] P3-1 `HonorFirmware.SetMode` FRI 仅接受 0/1；HUNTER 返回明确错误且不写 `7/8`。
- [ ] P3-2 `Broker/PerformanceController.ApplyManualAsync` 加 `HnPerformanceCenter`/`HnPerfPowerNexus` 冲突检测，返回“请先退出电脑管家”的可读原因。
- [ ] P3-3 `Snapshot`/`FirmwareStatus` 输出 `ModeCode`、`ModeName`、`ModeWrite`、`TurboXAvailable`。
- [ ] P3-4 UI 只显示智能/高能两个按钮，隐藏 HUNTER；显示当前模式与高能前置条件。
- [ ] P3-5 写入后 `4/14` 回读；若被电脑管家恢复，返回原因并恢复 UI。
- [ ] P3-6 手工验证：电脑管家退出后智能→高能→智能；接电/电量条件；模式保持至少 30 秒。

**P3 通过标准：** 电脑管家退出时智能/高能切换成功并可回读；电脑管家运行时给出明确阻断原因而不误写；HUNTER 不可用；运行中无风扇异常。

### P4 显示与色彩基础适配（P3 功能优先级）

- [ ] P4-1 新增 `BackendSource/DisplayGamma.cs`：`Get/SetDeviceGammaRamp`、按显示器枚举、保存/恢复原 ramp。
- [ ] P4-2 复用 `DisplayEffects` 的 `off|eye|reading` 与色温计算，输出 gamma ramp；不调用 `ControlLib.dll`。
- [ ] P4-3 `PlatformProfile`/`Snapshot` 暴露 `DisplayGamma`；`BackendClient` 增加 `display-gamma-*` 命令。
- [ ] P4-4 `MainWindow.xaml` 精简 DisplayPage：刷新率、护眼、阅读、色温；隐藏硬件 LUT、ICC、ColorProfileCard。
- [ ] P4-5 应用退出/窗口关闭恢复原 gamma；切换失败不静默。
- [ ] P4-6 验证 AMD Radeon 760M 下 gamma 生效、可恢复；刷新率若不可切换则只读显示。

**P4 通过标准：** 护眼/阅读/色温可应用并恢复；不依赖 Intel 驱动；显示页不出现不可用的原厂校准选项；截图更新。

### P5 收尾、文档与发布验证

- [ ] P5-1 README/INSTALL/CHANGELOG/版本号 `0.15.0` 同步。
- [ ] P5-2 `dotnet run --project Tests/PolicyChecks -c Release -- manual-performance`
- [ ] P5-3 `dotnet run --project Tests/PolicyChecks -c Release`（完整，含 GPU idle 等待）
- [ ] P5-4 `dotnet run --project Tests/LocalizationChecks`
- [ ] P5-5 `.\Build.ps1 -OutputDirectory .\artifacts\HonorControl -WithoutCalibration`
- [ ] P5-6 `.\Build-Installer.ps1 -PackageDirectory .\artifacts\HonorControl`
- [ ] P5-7 安装到本机，复核界面能力折叠、充电、Turbo X、显示；卸载验证保留设置/日志。
- [ ] P5-8 更新截图并提交。

## 5. 验证计划

### 5.1 自动验证

按 AGENTS.md 的 CI 顺序：

1. `dotnet run --project Tests/PolicyChecks -c Release -- manual-performance`
2. `dotnet run --project Tests/LocalizationChecks`
3. `.\Build.ps1 -OutputDirectory .\artifacts\HonorControl -WithoutCalibration`
4. `.\Build-Installer.ps1 -PackageDirectory .\artifacts\HonorControl`

新增 FRI 能力矩阵测试必须覆盖：`Detect()` 只接受 FRI-H56、拒绝 DRA 与其他机型；`ModeWrite/ChargeWrite=true` 但 `FanPreset/Hunter/DiscreteGpu/Dtt/Audio/Fn/MicLight/ColorLut=false`；`Network=false`、`DisplayGamma=true`；`AssertWrites` 对不支持的写路径仍抛错。

### 5.2 手工验证矩阵

| 场景 | 预期 |
| --- | --- |
| 本机启动 | 只显示充电、Turbo X、精简显示；网络/音频/风扇/HUNTER/GPU/DTT 不可见 |
| 点击充电 40–70 | 写入后 `3/17` 回读 start=40/stop=70；状态文案更新 |
| 点击充电 70–90 / 95–100 | 同上 |
| 电脑管家运行中切换 Turbo X | 提示“请先退出电脑管家”，不写入 |
| 电脑管家退出后切换高能 | `4/14` 返回 1，UI 显示高能；30 秒后仍为 1 |
| 拔电后切高能 | 拒绝并提示需接电/电量 ≥20% |
| DisplayPage 护眼/阅读/色温 | 屏幕变化，可恢复；无原厂 LUT 按钮 |
| 手工调用 `firmware-hunter`/`fan-3`/`network-configure` | Broker 拒绝并返回原因 |

### 5.3 恢复与异常

- 所有 WMI 写入前保存旧值，失败/异常必须恢复。
- `SetMode` 失败恢复旧模式；`SetCharge` 失败恢复旧阈值；gamma 退出恢复原 ramp。
- 任何无法回读的写入视为失败，不显示“成功”。
- 记录到 `Shared/DiagnosticLog.cs`，日志 2 MiB × 3 × 14 天。

## 6. 风险与待确认

| 风险 | 影响 | 处置 |
| --- | --- | --- |
| `3/16`/`3/17` 的实际阈值语义未知 | 可能写错或无效 | P0 先读、再小步写；以回读为准 |
| 电脑管家性能组件常驻 | Turbo X 写入被覆盖 | 要求退出电脑管家；P3 冲突检测 + 明确提示 |
| `HnPerformanceCenter`/`HnPerfPowerNexus` 可能与 DTT 冲突 | 现有 `DttPolicy` 会阻断 | FRI 不启用 DTT；Turbo X 手动路径单独处理 |
| FermiH 无原厂 ICC/LUT | 无法复刻原厂色彩 | 明确只做 gamma；不宣称原厂校准 |
| 无圆口电源、无风扇预设 | HUNTER/五档不可用 | 隐藏，不在 FRI 适配范围 |
| `Install.ps1` 默认不传 `--allow-writes` | 安装后写入仍禁用 | P1/P2 验证通过后，由用户授权再改安装参数 |
| 当前 shell 非管理员 | 无法直接验证 WMI 写 | P0/P2/P3 通过提升的 Broker 或管理员脚本执行 |
| 显示 gamma 可能与 Windows HDR/ACM 冲突 | 效果不稳定 | 只支持 SDR；HDR/ACM 下隐藏或提示 |

## 7. 完成定义（Definition of Done）

- 本机 `FRI-HXX / M1030 / BIOS 1.15 / 7640HS / Radeon 760M` 启动后只显示适配功能。
- 充电保护三组预设可写、可回读、可恢复。
- Turbo X 智能/高能可切换、可回读；电脑管家运行时有明确阻断提示，不误写。
- 显示页提供 gamma 级护眼/阅读/色温与刷新率；无原厂 LUT/ICC 误导。
- 网络加速、HUNTER、风扇预设、GPU、音频、DTT、Fn 写入全部隐藏或被 Broker 拒绝。
- `PolicyChecks`、`LocalizationChecks`、`Build.ps1`、`Build-Installer.ps1` 全部通过。
- README/INSTALL/CHANGELOG/版本号/截图同步到 `0.15.0`。

## 8. 执行顺序（一句话）

**P0 基线 → P1 平台/能力折叠 → P2 充电保护 → P3 Turbo X 智能/高能 → P4 显示 gamma → P5 文档/构建/安装验证。**

## 9. 执行记录（2026-11-17 至 2026-11-18）

- P0 已完成只读基线：`platform-status` 确认识别为 FRI-HXX / M1030 / BIOS 1.15 / 7640HS / Radeon 760M，管理员 WMI 探针已确认 4/14、3/17、2/8、2/2、2/9 读取值。
- P1/P2/P3/P4 代码已实现并通过编译：能力矩阵、充电状态校验、Turbo X 冲突拒绝、Windows gamma ramp 与 UI 能力折叠。
- P2 充电保护真实写入已验证：`40/70` 写入→回读→恢复；`60/65` 写入后电池在接电状态下从 77% 自然放电并稳定在 65%，持续约 1 小时；`65/70` 写入→回读，恢复 `MBAMainService` 后仍保持 70% 上限。当前实机阈值为 `65/70`。
- P3 Turbo X 真实写入已验证：停止 `MBAMainService`/性能组件后，`firmware-smart` 与 `firmware-performance` 均回读成功并恢复智能模式；HUNTER 未写入。
- P4 Windows gamma 已验证：`display-gamma-apply eye 20` 生效，`display-gamma-status` 报告 `Active=true`，`display-gamma-restore` 可恢复。
- P5 已完成 README/INSTALL/CHANGELOG/版本号 `0.15.0` 同步；PolicyChecks（manual + full）、LocalizationChecks、Build.ps1、Build-Installer.ps1 通过。
- 已安装 `artifacts\setup\HonorControl-0.15.0-Setup-x64.exe` 到 `C:\Program Files\HonorControl`；启动向导、性能页、显示页、托盘菜单均按 FRI 能力折叠。安装后的 Broker 使用 `--allow-writes`，并通过 UI 充电预设完成一次 `40/70` 写入验证，随后恢复 `65/70`。
- 最终安装包 SHA256：`5316A616C9984FCE54EB35EA3445ED4DA788F9B764BD5952ED0F4E27938F1EC5`。实机截图：`artifacts\ui-performance.png`、`artifacts\ui-display-final.png`、`artifacts\ui-guide-new.png`。
