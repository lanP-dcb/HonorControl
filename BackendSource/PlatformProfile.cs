using System;
using System.Linq;
using System.Management;

// Detected platform and the feature set verified for it. Reads only; no firmware access.
internal sealed class PlatformProfile {
    public string Name="Unsupported";
    public string Display="";
    public string Detail="";
    public string Model="";
    public string Bios="";
    public string Cpu="";
    public string Gpu="";
    public bool FirmwareWrites;
    public bool Hunter;
    public bool FanPreset;
    public bool DiscreteGpu;
    public bool BarrelJack;
    public bool Dtt;
    public bool SecondFan;
    public bool ModeWrite;
    public bool ChargeWrite;
    public bool FnWrite;
    public bool MicLight;
    public bool ColorLut;
    public bool Audio;
    public string WriteBlockReason="";

    static PlatformProfile current;
    public static PlatformProfile Current { get { if(current==null)current=Detect();return current; } }

    static string Read(string query,string property) {
        using(var search=new ManagementObjectSearcher(query))using(var all=search.Get())
            return all.Cast<ManagementObject>().Select(x=>Convert.ToString(x[property])).FirstOrDefault(v=>!String.IsNullOrEmpty(v))??"";
    }

    public static PlatformProfile Detect() {
        var profile=new PlatformProfile();
        try {
            profile.Model=Read("SELECT Model FROM Win32_ComputerSystem","Model");
            profile.Bios=Read("SELECT SMBIOSBIOSVersion FROM Win32_BIOS","SMBIOSBIOSVersion");
            profile.Cpu=Read("SELECT Name FROM Win32_Processor","Name");
            profile.Gpu=String.Join(" | ",new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController").Get().Cast<ManagementObject>().Select(x=>Convert.ToString(x["Name"])).Where(v=>!String.IsNullOrEmpty(v)).ToArray());
        } catch(Exception ex) {
            profile.Name="Unsupported";profile.WriteBlockReason="Platform detection failed: "+ex.Message;return profile;
        }
        if(profile.Model=="DRA-XX"&&profile.Bios=="1.13"&&profile.Cpu.Contains("125H")&&profile.Gpu.Contains("RTX 4060")) {
            profile.Name="DRA-XX";profile.FirmwareWrites=true;
            profile.Display="HONOR MagicBook Pro 16";profile.Detail="Core Ultra 5 125H / GeForce RTX 4060";
            profile.ModeWrite=true;profile.ChargeWrite=true;profile.FnWrite=true;profile.MicLight=true;profile.ColorLut=true;profile.Audio=true;
            profile.Hunter=true;profile.FanPreset=true;profile.DiscreteGpu=true;profile.BarrelJack=true;profile.Dtt=true;profile.SecondFan=true;
            return profile;
        }
        if(profile.Model=="FRI-HXX"&&profile.Bios=="1.15"&&profile.Cpu.Contains("7640HS")&&profile.Gpu.Contains("Radeon 760M")) {
            profile.Name="FRI-HXX";profile.FirmwareWrites=true;
            profile.Display="HONOR MagicBook X14 2024";profile.Detail="Ryzen 5 7640HS / Radeon 760M";
            // Reads are verified on this platform; every write stays closed until each one is proven.
            profile.ModeWrite=false;profile.ChargeWrite=false;profile.FnWrite=false;profile.MicLight=false;profile.ColorLut=false;profile.Audio=false;
            profile.Hunter=false;profile.FanPreset=false;profile.DiscreteGpu=false;profile.BarrelJack=false;profile.Dtt=false;profile.SecondFan=false;
            return profile;
        }
        profile.Name="Unsupported";
        profile.Display=profile.Model;profile.Detail=profile.Cpu;
        profile.WriteBlockReason="Writes are limited to verified platforms. Detected "+profile.Model+" / BIOS "+(profile.Bios.Length==0?"unknown":profile.Bios)+" / "+profile.Cpu+" / "+profile.Gpu+".";
        return profile;
    }

    public void AssertWrites() {
        if(!FirmwareWrites)throw new InvalidOperationException(WriteBlockReason.Length>0?WriteBlockReason:"Writes are limited to verified platforms.");
    }

    public object Snapshot() {
        return new {Profile=Name,Display=Display,Detail=Detail,Model=Model,Bios=Bios,Cpu=Cpu,Gpu=Gpu,FirmwareWrites=FirmwareWrites,Hunter=Hunter,FanPreset=FanPreset,DiscreteGpu=DiscreteGpu,BarrelJack=BarrelJack,Dtt=Dtt,SecondFan=SecondFan,ModeWrite=ModeWrite,ChargeWrite=ChargeWrite,FnWrite=FnWrite,MicLight=MicLight,ColorLut=ColorLut,Audio=Audio};
    }
}
