# Honor Control

[简体中文](README.md) · [Downloads](https://github.com/wzk112/HonorControl/releases) · [Installation](INSTALL.en-US.md) · [Changelog](CHANGELOG.md)

Independent WinUI 3 device controls for the **HONOR MagicBook X14 2024 (FRI-H56 / FRI-HXX / M1030 / BIOS 1.15 / Ryzen 5 7640HS / Radeon 760M)**, without running HONOR PC Manager. Chinese and English UI are included.

**Source available for noncommercial personal use and personal research only. Commercial use is prohibited.** See the [Honor Control Personal Research License 1.0](LICENSE). This is not an OSI open-source license. This project is independent of HONOR.

> Screenshots will be updated after validation on the FRI-H56 device.

## Features

- Charging protection presets: home 40–70%, office 70–90%, travel 95–100%; reads battery, AC state and current thresholds.
- Smart and Performance (Turbo X) modes; firmware mode, temperature and RPM readback; warns you to exit HONOR PC Manager while it owns performance control.
- Internal refresh-rate selection; eye-care, reading and colour-temperature effects through the Windows system gamma ramp, with one-click restore.
- Device identification limited to FRI-HXX / M1030 / BIOS 1.15 / Ryzen 5 7640HS / Radeon 760M. Other models are read-only with a clear reason.
- Program Files installation, sign-in startup, tray operation, background recovery, tutorial, changelog and About page.
- Not enabled for this model: network acceleration, audio, Fn, GPU switching, DTT/OS Turbo, automatic performance, process scheduling, HUNTER and fan presets.

## Install

Download `HonorControl-0.15.0-Setup-x64.exe` from [Releases](https://github.com/wzk112/HonorControl/releases), double-click it and approve UAC once. The installer handles files, the Start menu and sign-in tasks. No manual PowerShell step is required.

The target is `C:\Program Files\HonorControl` with two sign-in tasks, `HonorControl.Agent` and `HonorControl.UI`. Start from the Start menu. Closing the window keeps the tray active. Exiting from the tray pauses until next sign-in or manual launch. Upgrades preserve preferences.

The HONOR PC Manager package for this model does not provide factory ICC, LUT or colour-management data for the FRI/H family, so Setup does not import factory colour data; display effects use the Windows system gamma ramp. Public packages omit OEM DLLs, drivers and calibration data.

Uninstall HONOR PC Manager, separate OSD, HUNTERCAMP, Magic Animation and YOYO software if replacing their controls. Keep Intel, NVIDIA, Realtek, Fortemedia, Histen, ACPI and other hardware drivers, audio services and calibration folders. The cleanup script previews its exact scope by default and only applies with `-Apply`. See the [migration guide](INSTALL.en-US.md).

## Compatibility and limitations

The verified device is FRI-HXX / M1030 / BIOS 1.15 / Ryzen 5 7640HS / Radeon 760M. Firmware writes are guarded and only enabled after explicit authorization. Performance mode needs AC and at least 20% battery. HONOR PC Manager must not be actively managing performance mode at the same time.

Other CPU, BIOS or model combinations are read-only and report the detected configuration. Display and colour use the Windows system gamma ramp only: this model has no factory hardware LUT, ICC or colour-management data, so Native/sRGB/P3 hardware switching is not offered. Network acceleration, audio, Fn, GPU switching, DTT/OS Turbo, automatic performance, process scheduling, HUNTER and fan presets are not enabled for this model.

Display effects are limited to the Windows system gamma ramp. HDR and automatic colour management are not validated. Network acceleration, audio, Fn, GPU switching, DTT/OS Turbo, automatic performance, process scheduling, HUNTER and fan presets are intentionally out of scope for this model and are hidden by the capability profile.

## Build and data

Windows x64, .NET 8 SDK and Windows desktop build prerequisites:

```powershell
.\Build.ps1 -OutputDirectory .\artifacts\HonorControl -WithoutCalibration
```

`App` contains WinUI, `Broker` the privileged allowlisted commands and controllers, `Shared` the protocol / scene tables, and `BackendSource` the installed-driver interfaces. Output includes .NET and Windows App SDK runtimes. This model has no factory colour calibration data; display adjustment uses the Windows system gamma ramp.

Preferences, logs and recovery records stay in `%LOCALAPPDATA%\HonorControl`. There is no automatic log upload or update check. Documentation links open a browser; network features access the network when used. Personal state, user SIDs, registry / recovery backups, OEM binaries and calibration data are excluded from the repository. Dependencies retain their own licenses; see [third-party notices](THIRD-PARTY-NOTICES.md).

Build the installer with Build-Installer.ps1 -PackageDirectory .\artifacts\HonorControl. Uninstall through Windows Installed apps. Preferences, logs, calibration and hardware drivers are retained.

Runtime logs rotate at 2 MiB with three files per stream. Logs not updated for 14 days are removed. Recovery records, preferences and calibration backups are retained.
