using System;
using System.Linq;
using System.Management;
using HonorControl.Shared;

// Resolves the machine identity once and exposes only the capabilities that the
// simplified FRI-H56 build actually uses.
internal sealed class PlatformProfile
{
    public string Name = "Unsupported";
    public string Display = "Unknown model";
    public string Detail = "";
    public string Model = "";
    public string Version = "";
    public string Bios = "";
    public string Cpu = "";
    public string Gpu = "";
    public bool FirmwareWrites;
    public bool ModeWrite;
    public bool ChargeWrite;
    public bool DisplayGamma;
    public bool FnWrite;
    public string WriteBlockReason = "";

    private static PlatformProfile current;
    public static PlatformProfile Current { get { return current ?? (current = Detect()); } }

    private static string Read(string query, string property)
    {
        using (var searcher = new ManagementObjectSearcher(query))
        using (var results = searcher.Get())
        {
            foreach (ManagementObject item in results)
            {
                string value = Convert.ToString(item[property]);
                if (!String.IsNullOrWhiteSpace(value)) return value.Trim();
            }
        }
        return "";
    }

    public static PlatformProfile Detect()
    {
        string model = Read("SELECT Model FROM Win32_ComputerSystem", "Model");
        string version = Read("SELECT Version FROM Win32_ComputerSystemProduct", "Version");
        string bios = Read("SELECT SMBIOSBIOSVersion FROM Win32_BIOS", "SMBIOSBIOSVersion");
        string cpu = Read("SELECT Name FROM Win32_Processor", "Name");
        string gpu = string.Join(" | ", new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController")
            .Get().Cast<ManagementObject>().Select(x => Convert.ToString(x["Name"])).Where(x => !String.IsNullOrWhiteSpace(x)));
        return ForIdentity(model, version, bios, cpu, gpu);
    }

    public static PlatformProfile ForIdentity(string model, string version, string bios, string cpu, string gpu)
    {
        var caps = PlatformCapabilities.ForIdentity(model, version, bios, cpu, gpu);
        return new PlatformProfile
        {
            Name = caps.Name,
            Display = caps.Display,
            Detail = caps.Detail,
            Model = model,
            Version = version,
            Bios = bios,
            Cpu = cpu,
            Gpu = gpu,
            FirmwareWrites = caps.FirmwareWrites,
            ModeWrite = caps.ModeWrite,
            ChargeWrite = caps.ChargeWrite,
            DisplayGamma = caps.DisplayGamma,
            FnWrite = caps.FnWrite,
            WriteBlockReason = caps.WriteBlockReason
        };
    }

    public void AssertWrites()
    {
        if (!FirmwareWrites)
            throw new NotSupportedException(String.IsNullOrWhiteSpace(WriteBlockReason)
                ? "Firmware writes are not enabled for this machine."
                : WriteBlockReason);
    }

    public object Snapshot()
    {
        return new
        {
            Profile = Name,
            Display,
            Detail,
            Model,
            Version,
            Bios,
            Cpu,
            Gpu,
            FirmwareWrites,
            ModeWrite,
            ChargeWrite,
            DisplayGamma,
            FnWrite,
            Supported = Name == "FRI-HXX",
            WriteBlockReason
        };
    }
}
