# Installation and migration

Honor Control 0.15.0 targets Windows x64 on the HONOR MagicBook X14 2024 (FRI-H56 / FRI-HXX / M1030 / BIOS 1.15 / Ryzen 5 7640HS / Radeon 760M). Firmware writes are disabled until explicitly authorized. Install under your usual signed-in administrator account, rather than entering another account's credentials.

## Calibration

The HONOR PC Manager package for this model does not provide factory ICC, LUT or colour-management data for the FRI/H family, so Setup does not import factory colour data. Display effects use the Windows system gamma ramp and can be restored to the original settings.

The calibration import script remains available for other models. This model has no factory calibration data to import, so you do not need to run `Import-Calibration.ps1`.

## Install or upgrade

Download HonorControl-0.15.0-Setup-x64.exe from Releases. Double-click, approve UAC and choose Install. The setup handles Program Files, Start menu and the HonorControl.UI / HonorControl.Agent sign-in tasks. There is no manual PowerShell step. Use your usual signed-in administrator account.

Open from Start. The tutorial includes usage steps, specific OEM service names and live service detection. Change Chinese / English in About and restart the UI. Upgrades preserve preferences and calibration. Uninstall through Windows Installed apps. Personal data and drivers are retained; calibration is cached in LocalAppData\HonorControl\Calibration and reused on reinstall. Developers can still use Install.ps1 for extracted build output.
## Remove OEM software

First verify Honor Control. Use Windows Installed apps / Programs and Features to uninstall HONOR PC Manager and separate OSD, HUNTERCAMP, Magic Animation and YOYO software. Reboot if requested. See [Microsoft uninstall instructions](https://support.microsoft.com/en-gb/windows/uninstall-or-remove-apps-and-programs-in-windows-4b55f974-2cc6-2d2b-d092-5905080eaf98) and [HONOR instructions](https://www.honor.com/cn/support/content/zh-cn15813919/).

Preview the remaining software files and registrations:

```powershell
.\Clean-HonorSoftware.ps1
```

Apply the printed scope from administrator PowerShell:

```powershell
.\Clean-HonorSoftware.ps1 -Apply
```

Targets exact HONOR application folders, software services / tasks / startup references, known uninstall entries, three OEM Appx packages and selected application registry branches. PnP devices, DriverStore and audio calibration are excluded. Reports and registry backups stay in %LOCALAPPDATA%\HonorControl\Migration. No automatic reboot. Locked leftovers are reported.

| Keep | Needed for |
| --- | --- |
| ACPI, WMI, chipset and firmware | Modes, fans, Fn and charging |
| Intel Dynamic Tuning | Scene power policy |
| Intel graphics and matching ICC / LUT | Color |
| NVIDIA display driver | Discrete GPU control and offsets |
| Histen / Honor audio APO, Awinic and calibration | Audio effects |
| Fortemedia APO and FMAPOService | Microphone pickup |
| Realtek, Intel Smart Sound and audio services | Audio endpoints |
| Camera, HID, network and device drivers | Physical devices |

An HONOR vendor name in a driver is not sufficient reason to delete it. ProgramData\HonorEffect and Windows driver folders are required by audio functions.

## Check and recover

After sign-in, check tray / hardware connection, modes and HUNTER level, Fn+P / Fn+R notifications, touchpad / microphone behavior and audio. Color needs both hardware LUT and ICC association.

Automatic performance, process QoS and OS Turbo are separate switches. A recognized game retains the highest available mode while its process runs. The 45-second grace period starts when recognized processes disappear. Stabilization and minimum switch intervals also apply.

Closing the window keeps the tray active; Exit pauses this session. Disabling a controller restores changes it owns from recovery records. Restore original backup restores saved hardware color and ICC. Vendor software and drivers can be reinstalled from HONOR for the exact model. Avoid two automatic performance controllers at once. Feature gaps and verification limits are in [README](README.en-US.md).
