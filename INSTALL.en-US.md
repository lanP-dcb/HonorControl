# Installation and migration

Honor Control 0.15.2 targets Windows x64 on the HONOR X14 Ryzen Edition 2024 R5-7640HS (FRI-H56; model code FRI-HXX; supported BIOS 1.15). The installer enables Broker writes, but only capability-approved charge and Balanced/Performance controls are executed. Install under your usual signed-in administrator account, rather than entering another account's credentials.

## Calibration

The HONOR PC Manager package for this model does not provide factory ICC, LUT or colour-management data for the FRI/H family, so Setup does not import factory colour data. Display effects use the Windows system gamma ramp and can be restored to the original settings.

This model has no factory calibration data to import and does not use ICC/LUT imports; display adjustment uses the Windows system gamma ramp directly.

## Install or upgrade

Download HonorControl-0.15.2-Setup-x64.exe from Releases. Double-click, approve UAC and choose Install. The setup handles Program Files, Start menu and the HonorControl.UI / HonorControl.Agent sign-in tasks. There is no manual PowerShell step. The finish page offers an optional "Run Honor Control" checkbox; setup does not launch the app unless it is ticked when you click OK. Use your usual signed-in administrator account.

Open from Start. The tutorial includes usage steps and OEM migration notes. Change Chinese / English in About and restart the UI. Upgrades preserve preferences and logs. Uninstall through Windows Installed apps. Personal data, logs and drivers are retained. Developers can still use Install.ps1 for extracted build output.

## OEM components and removal

The OEM performance, colour and OSD backends may write the same device settings as Honor Control. Install and verify Honor Control first, then uninstall HONOR PC Manager from Windows Installed apps / Programs and Features. Remove separate OSD, HUNTERCAMP, Magic Animation and YOYO software as needed. See [Microsoft uninstall instructions](https://support.microsoft.com/en-gb/windows/uninstall-or-remove-apps-and-programs-in-windows-4b55f974-2cc6-2d2b-d092-5905080eaf98) and [HONOR instructions](https://www.honor.com/cn/support/content/zh-cn15813919/).

After uninstalling HONOR PC Manager, Honor Control does not depend on `PCManager.exe`, `MBAMainService`, `HnPerformanceCenter`, `HnPerfPowerNexus`, `HnLcdEnhancement`, `SmartChargePlugin` or ICC/LUT files. Charge and Balanced/Performance use the local ACPI/WMI firmware interface; display gamma uses Windows APIs.

Keep these low-level components:

| Keep | Needed for |
| --- | --- |
| `ACPI\PNP0C14\HWMI` ACPI/WMI device (Windows `wmiacpi`) and BIOS/UEFI firmware | Charge protection and Balanced/Performance |
| `ACPI\PNP0C14\HWMI` ACPI/WMI device | Fn / F1鈥揊12 priority |
| Windows system gamma ramp | Eye-care, reading mode and colour temperature; no `HnLcdEnhancement`, ICC or hardware LUT |
| Audio, fan presets, GPU switching and DTT | Removed in this release; corresponding modules are not loaded |

Do not delete the `ACPI\PNP0C14\HWMI` device, AMD graphics driver, HID or chipset drivers. An HONOR vendor name can also appear in camera or audio drivers; deleting those affects the corresponding hardware.

Recommended removal order:

1. Install and verify Honor Control: charge thresholds, Balanced/Performance, Fn and gamma.
2. Exit the HONOR PC Manager performance backend; confirm `HnPerformanceCenter` and `HnPerfPowerNexus` are gone. `MBAProcessWatcher` may restart them; stop `MBAMainService` if necessary.
3. Uninstall HONOR PC Manager from Windows Installed apps, then reboot.
4. Confirm `ACPI\PNP0C14\HWMI` is still healthy in Device Manager; run `platform-status` and `charge-status`, then test one charge write, Balanced/Performance switching and gamma restore.
5. Use the bundled cleanup script only after verification. It previews by default; apply the printed scope from administrator PowerShell with `-Apply`.

```powershell
.\Clean-HonorSoftware.ps1
.\Clean-HonorSoftware.ps1 -Apply
```

If WMI returns access denied or the device is missing after removal, reinstall the HONOR ACPI/BIOS driver package for this exact model rather than the whole PC Manager. The cleanup script does not delete DriverStore, PnP devices, `ProgramData\HonorEffect` or audio drivers, and does not reboot; reports and registry backups stay in `%LOCALAPPDATA%\HonorControl\Migration`.

## Check and recover

After sign-in, confirm the tray and hardware connection; About should report the HONOR X14 Ryzen Edition 2024 R5-7640HS and supported BIOS 1.15. Balanced, Performance and a display-only HUNTER mode should appear; HUNTER cannot be selected and Performance requires AC and at least 20% battery. Charging should show the current battery percentage, AC state and thresholds, including the custom preset and master switch. Display & colour should offer eye-care, reading and temperature only; restoring gamma returns the panel to its original state. Network, audio, GPU, fan presets and DTT have been removed from this release and are absent from the UI and package.

Closing the window keeps the tray active; Exit pauses this session. Disabling charge or performance control restores changes it owns from recovery records; exiting restores the original Windows gamma ramp. Vendor software and drivers can be reinstalled from HONOR for the exact model. Feature gaps and verification limits are in [README](README.en-US.md).
