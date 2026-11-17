# Installation and migration

Honor Control 0.15.0 targets Windows x64 on the HONOR MagicBook X14 2024 (FRI-H56 / FRI-HXX / M1030 / BIOS 1.15 / Ryzen 5 7640HS / Radeon 760M). The installer enables Broker writes, but only capability-approved charge and performance controls are executed. Install under your usual signed-in administrator account, rather than entering another account's credentials.

## Calibration

The HONOR PC Manager package for this model does not provide factory ICC, LUT or colour-management data for the FRI/H family, so Setup does not import factory colour data. Display effects use the Windows system gamma ramp and can be restored to the original settings.

The calibration import script remains available for other models. This model has no factory calibration data to import, so you do not need to run `Import-Calibration.ps1`.

## Install or upgrade

Download HonorControl-0.15.0-Setup-x64.exe from Releases. Double-click, approve UAC and choose Install. The setup handles Program Files, Start menu and the HonorControl.UI / HonorControl.Agent sign-in tasks. There is no manual PowerShell step. Use your usual signed-in administrator account.

Open from Start. The tutorial includes usage steps, specific OEM service names and live service detection. Change Chinese / English in About and restart the UI. Upgrades preserve preferences and calibration. Uninstall through Windows Installed apps. Personal data and drivers are retained; calibration is cached in LocalAppData\HonorControl\Calibration and reused on reinstall. Developers can still use Install.ps1 for extracted build output.

## OEM components and removal

The OEM performance, colour and OSD backends may write the same device settings as Honor Control. Install and verify Honor Control first, then uninstall HONOR PC Manager from Windows Installed apps / Programs and Features. Remove separate OSD, HUNTERCAMP, Magic Animation and YOYO software as needed. See [Microsoft uninstall instructions](https://support.microsoft.com/en-gb/windows/uninstall-or-remove-apps-and-programs-in-windows-4b55f974-2cc6-2d2b-d092-5905080eaf98) and [HONOR instructions](https://www.honor.com/cn/support/content/zh-cn15813919/).

After uninstalling HONOR PC Manager, Honor Control does not depend on `PCManager.exe`, `MBAMainService`, `HnPerformanceCenter`, `HnPerfPowerNexus`, `HnLcdEnhancement`, `SmartChargePlugin` or ICC/LUT files. Charge and Smart/High use the local ACPI/WMI firmware interface; display gamma uses Windows APIs.

Keep these low-level components:

| Keep | Needed for |
| --- | --- |
| `ACPI\PNP0C14\HWMI` ACPI/WMI device (Windows `wmiacpi`) and BIOS/UEFI firmware | Charge protection and Smart/High |
| AMD Radeon graphics driver and Windows display configuration | Internal-panel refresh rate |
| Windows system gamma ramp | Eye-care, reading mode and colour temperature; no `HnLcdEnhancement`, ICC or hardware LUT |
| Audio, Fn, fan presets, GPU switching and DTT | Not enabled in this FRI build |

Do not delete the `ACPI\PNP0C14\HWMI` device, AMD graphics driver, HID or chipset drivers. An HONOR vendor name can also appear in camera or audio drivers; deleting those affects the corresponding hardware.

Recommended removal order:

1. Install and verify Honor Control: charge thresholds, Smart/High, gamma and refresh rate.
2. Exit the HONOR PC Manager performance backend; confirm `HnPerformanceCenter` and `HnPerfPowerNexus` are gone. `MBAProcessWatcher` may restart them; stop `MBAMainService` if necessary.
3. Uninstall HONOR PC Manager from Windows Installed apps, then reboot.
4. Confirm `ACPI\PNP0C14\HWMI` is still healthy in Device Manager; run `platform-status` and `charge-status`, then test one charge write, Smart/High switching and gamma restore.
5. Use the bundled cleanup script only after verification. It previews by default; apply the printed scope from administrator PowerShell with `-Apply`.

```powershell
.\Clean-HonorSoftware.ps1
.\Clean-HonorSoftware.ps1 -Apply
```

If WMI returns access denied or the device is missing after removal, reinstall the HONOR ACPI/BIOS driver package for this exact model rather than the whole PC Manager. The cleanup script does not delete DriverStore, PnP devices, `ProgramData\HonorEffect` or audio drivers, and does not reboot; reports and registry backups stay in `%LOCALAPPDATA%\HonorControl\Migration`.

## Check and recover

After sign-in, confirm the tray and hardware connection; About should report `FRI-HXX / BIOS 1.15`. Only Smart and High modes should appear; High requires AC and at least 20% battery. Charging should show the current battery percentage, AC state and thresholds. Display & colour should offer eye-care, reading, temperature and refresh rate only; restoring gamma returns the panel to its original state. Network, audio, Fn, GPU, fan presets, HUNTER and DTT are intentionally hidden by the capability profile.

Closing the window keeps the tray active; Exit pauses this session. Disabling charge or performance control restores changes it owns from recovery records; exiting restores the original Windows gamma ramp. Vendor software and drivers can be reinstalled from HONOR for the exact model. Feature gaps and verification limits are in [README](README.en-US.md).
