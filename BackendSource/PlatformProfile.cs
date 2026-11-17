using System;
using System.Linq;
using System.Management;
using HonorControl.Shared;

// Detected platform and the feature set verified for it. Reads only; no firmware access.
internal sealed class PlatformProfile {
    public string Name="Unsupported";
    public string Display="";
    public string Detail="";
    public string Model="";
    public string Version="";
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
    public bool DisplayGamma;
    public bool Network;
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

    public static PlatformProfile ForIdentity(string model,string version,string bios,string cpu,string gpu) {
        var capabilities=PlatformCapabilities.ForIdentity(model,version,bios,cpu,gpu);
        return new PlatformProfile {
            Name=capabilities.Name,Display=capabilities.Display,Detail=capabilities.Detail,
            Model=model,Version=version,Bios=bios,Cpu=cpu,Gpu=gpu,
            FirmwareWrites=capabilities.FirmwareWrites,ModeWrite=capabilities.ModeWrite,
            ChargeWrite=capabilities.ChargeWrite,DisplayGamma=capabilities.DisplayGamma,
            Network=capabilities.Network,Hunter=capabilities.Hunter,FanPreset=capabilities.FanPreset,
            DiscreteGpu=capabilities.DiscreteGpu,Dtt=capabilities.Dtt,SecondFan=capabilities.SecondFan,
            FnWrite=capabilities.FnWrite,MicLight=capabilities.MicLight,ColorLut=capabilities.ColorLut,
            Audio=capabilities.Audio,WriteBlockReason=capabilities.WriteBlockReason
        };
    }

    public static PlatformProfile Detect() {
        string model,version,bios,cpu,gpu;
        try {
            model=Read("SELECT Model FROM Win32_ComputerSystem","Model");
            version=Read("SELECT Version FROM Win32_ComputerSystemProduct","Version");
            bios=Read("SELECT SMBIOSBIOSVersion FROM Win32_BIOS","SMBIOSBIOSVersion");
            cpu=Read("SELECT Name FROM Win32_Processor","Name");
            gpu=String.Join(" | ",new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController").Get().Cast<ManagementObject>().Select(x=>Convert.ToString(x["Name"])).Where(v=>!String.IsNullOrEmpty(v)).ToArray());
        } catch(Exception ex) {
            return new PlatformProfile{Name="Unsupported",WriteBlockReason="Platform detection failed: "+ex.Message};
        }
        return ForIdentity(model,version,bios,cpu,gpu);
    }

    public void AssertWrites() {
        if(!FirmwareWrites)throw new InvalidOperationException(WriteBlockReason.Length>0?WriteBlockReason:"Writes are limited to verified platforms.");
    }

    public object Snapshot() {
        return new {Profile=Name,Display=Display,Detail=Detail,Model=Model,Version=Version,Bios=Bios,Cpu=Cpu,Gpu=Gpu,FirmwareWrites=FirmwareWrites,Hunter=Hunter,FanPreset=FanPreset,DiscreteGpu=DiscreteGpu,BarrelJack=BarrelJack,Dtt=Dtt,SecondFan=SecondFan,ModeWrite=ModeWrite,ChargeWrite=ChargeWrite,DisplayGamma=DisplayGamma,Network=Network,FnWrite=FnWrite,MicLight=MicLight,ColorLut=ColorLut,Audio=Audio};
    }
}
