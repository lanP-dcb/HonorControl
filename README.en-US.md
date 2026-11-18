# Honor Control

[简体中文](README.md) · [Downloads](https://github.com/wzk112/HonorControl/releases) · [Installation](INSTALL.en-US.md) · [Changelog](CHANGELOG.md)

Independent WinUI 3 device controls for the **HONOR MagicBook X14 2024 (FRI-H56 / FRI-HXX / M1030 / BIOS 1.15 / Ryzen 5 7640HS / Radeon 760M)**, without running HONOR PC Manager. Chinese and English UI are included.

**Source available for noncommercial personal use and personal research only. Commercial use is prohibited.** See the [Honor Control Personal Research License 1.0](LICENSE). This is not an OSI open-source license. This project is independent of HONOR.

> Screenshots will be updated after validation on the FRI-H56 device.

## Features

- Charging protection presets: home 40–70%, office 70–90%, travel 95–100% and custom; the master switch defaults to Home when on and restores charge-to-full when off; reads battery, AC state and firmware readback.
- Smart and Performance (Turbo X) modes; HUNTER is display-only and cannot be selected; warns you to exit HONOR PC Manager while it owns performance control.
- Eye-care, reading and colour-temperature effects through the Windows system gamma ramp, with one-click restore; no refresh-rate control.
- Device identification limited to FRI-HXX / M1030 / BIOS 1.15 / Ryzen 5 7640HS / Radeon 760M. Other models get a first-run notice, a non-closable red banner and display-only controls.
- Program Files installation, sign-in startup, tray operation, background recovery, Fn / F1–F12 priority setting, tutorial, changelog and About page.
- Removed in this release: network acceleration, audio, GPU switching, DTT/OS Turbo, automatic performance, process scheduling and fan presets; HUNTER is display-only.

## Install

Download `HonorControl-0.15.0-Setup-x64.exe` from [Releases](https://github.com/wzk112/HonorControl/releases), double-click it and approve UAC once. The installer handles files, the Start menu and sign-in tasks. No manual PowerShell step is required.

The target is `C:\Program Files\HonorControl` with two sign-in tasks, `HonorControl.Agent` and `HonorControl.UI`. Start from the Start menu. Closing the window keeps the tray active. Exiting from the tray pauses until next sign-in or manual launch. Upgrades preserve preferences.

The HONOR PC Manager package for this model does not provide factory ICC, LUT or colour-management data for the FRI/H family, so Setup does not import factory colour data; display effects use the Windows system gamma ramp. Public packages omit OEM DLLs, drivers and calibration data.

Uninstall HONOR PC Manager, separate OSD, HUNTERCAMP, Magic Animation and YOYO software if replacing their controls. Keep the AMD Radeon graphics driver, the `ACPI\PNP0C14\HWMI` ACPI/WMI device, HID and chipset drivers; do not delete DriverStore or PnP devices. The cleanup script previews its exact scope by default and only applies with `-Apply`. See the [migration guide](INSTALL.en-US.md).

## Compatibility and limitations

The verified device is FRI-HXX / M1030 / BIOS 1.15 / Ryzen 5 7640HS / Radeon 760M. The 0.15.0 installer enables Broker `--allow-writes`, but only capability-approved charge and performance writes pass; firmware retains final control. Performance mode needs AC and at least 20% battery. HONOR PC Manager must not be actively managing performance mode at the same time.

Other CPU, BIOS or model combinations get a first-run notice, a non-closable red banner and display-only controls. Display and colour use the Windows system gamma ramp only: this model has no factory hardware LUT, ICC or colour-management data, so Native/sRGB/P3 hardware switching is not offered. Network acceleration, audio, GPU switching, DTT/OS Turbo, automatic performance, process scheduling and fan presets have been removed from this release; HUNTER is display-only.

Display effects are limited to the Windows system gamma ramp. HDR and automatic colour management are not validated. Network acceleration, audio, GPU switching, DTT/OS Turbo, automatic performance, process scheduling and fan presets are intentionally out of scope for this model and are hidden by the capability profile; HUNTER is display-only.

## Build and data

Windows x64, .NET 8 SDK and Windows desktop build prerequisites:

```powershell
.\Build.ps1 -OutputDirectory .\artifacts\HonorControl -WithoutCalibration
```

`App` contains WinUI, `Broker` the privileged allowlisted commands and background keep-alive, `Shared` the protocol, capability profile and runtime identity, and `BackendSource` the installed-driver interfaces. Output includes .NET and Windows App SDK runtimes. This model has no factory colour calibration data; display adjustment uses the Windows system gamma ramp.

Preferences, logs and recovery records stay in `%LOCALAPPDATA%\HonorControl`. There is no automatic log upload or update check. Documentation links open a browser; network features access the network when used. Personal state, user SIDs, registry / recovery backups, OEM binaries and calibration data are excluded from the repository. Dependencies retain their own licenses; see [third-party notices](THIRD-PARTY-NOTICES.md).

Build the installer with Build-Installer.ps1 -PackageDirectory .\artifacts\HonorControl. Uninstall through Windows Installed apps. Preferences, logs and hardware drivers are retained.

Runtime logs rotate at 2 MiB with three files per stream. Logs not updated for 14 days are removed. Recovery records, preferences and calibration backups are retained.
