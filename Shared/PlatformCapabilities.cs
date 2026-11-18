using System;

namespace HonorControl.Shared
{
// Pure identity-to-capability mapping. Kept free of WMI so it can be unit tested
// without touching firmware or the machine's actual hardware.
internal sealed class PlatformCapabilities
{
    public string Name = "Unsupported";
    public string Display = "";
    public string Detail = "";
    public bool FirmwareWrites;
    public bool ModeWrite;
    public bool ChargeWrite;
    public bool DisplayGamma;
    public bool Network;
    public bool Hunter;
    public bool FanPreset;
    public bool DiscreteGpu;
    public bool Dtt;
    public bool SecondFan;
    public bool FnWrite;
    public bool MicLight;
    public bool ColorLut;
    public bool Audio;
    public string WriteBlockReason = "";

    public static PlatformCapabilities ForIdentity(string model, string version, string bios, string cpu, string gpu)
    {
        var capabilities = new PlatformCapabilities();
        if (model == "FRI-HXX" && version == "M1030" && bios == "1.15" &&
            cpu.IndexOf("7640HS", StringComparison.Ordinal) >= 0 && gpu.IndexOf("Radeon 760M", StringComparison.Ordinal) >= 0)
        {
            capabilities.Name = "FRI-HXX";
            capabilities.Display = "HONOR MagicBook X14 2024";
            capabilities.Detail = "Ryzen 5 7640HS / Radeon 760M";
            capabilities.FirmwareWrites = true;
            capabilities.ModeWrite = true;
            capabilities.ChargeWrite = true;
            capabilities.DisplayGamma = true;
            capabilities.Network = false;
            capabilities.Hunter = false;
            capabilities.FanPreset = false;
            capabilities.DiscreteGpu = false;
            capabilities.Dtt = false;
            capabilities.SecondFan = false;
            capabilities.FnWrite = true;
            capabilities.MicLight = false;
            capabilities.ColorLut = false;
            capabilities.Audio = false;
            return capabilities;
        }

        capabilities.Display = model;
        capabilities.Detail = cpu;
        capabilities.WriteBlockReason = "Writes are limited to verified platforms. Detected " + model +
            " / version " + (version.Length == 0 ? "unknown" : version) +
            " / BIOS " + (bios.Length == 0 ? "unknown" : bios) +
            " / " + cpu + " / " + gpu + ".";
        return capabilities;
    }
}
}
