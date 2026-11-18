using System;

namespace HonorControl.Shared
{
    // The one place that decides which controls are meaningful for a machine.
    // Only the FRI-H56 build is supported; every other identity is display-only.
    internal sealed class PlatformCapabilities
    {
        public string Name = "Unsupported";
        public string Display = "Unknown model";
        public string Detail = "";
        public bool FirmwareWrites;
        public bool ModeWrite;
        public bool ChargeWrite;
        public bool DisplayGamma;
        public bool FnWrite;
        public string WriteBlockReason = "";

        public static PlatformCapabilities ForIdentity(string model, string version, string bios, string cpu, string gpu)
        {
            model = (model ?? "").Trim();
            version = (version ?? "").Trim();
            bios = (bios ?? "").Trim();
            cpu = cpu ?? "";
            gpu = gpu ?? "";

            if (model.Equals("FRI-HXX", StringComparison.OrdinalIgnoreCase) &&
                version.Equals("M1030", StringComparison.OrdinalIgnoreCase) &&
                bios.Equals("1.15", StringComparison.OrdinalIgnoreCase) &&
                cpu.IndexOf("7640HS", StringComparison.OrdinalIgnoreCase) >= 0 &&
                gpu.IndexOf("Radeon 760M", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new PlatformCapabilities
                {
                    Name = "FRI-HXX",
                    Display = "HONOR MagicBook X14 2024",
                    Detail = "Ryzen 5 7640HS / Radeon 760M",
                    FirmwareWrites = true,
                    ModeWrite = true,
                    ChargeWrite = true,
                    DisplayGamma = true,
                    FnWrite = true
                };
            }

            return new PlatformCapabilities
            {
                Name = "Unsupported",
                Display = model.Length == 0 ? "Unknown model" : model,
                Detail = "硬件与已验证的 FRI-H56 配置不一致。",
                WriteBlockReason = "Detected " + model + " / " + version + " / BIOS " + bios + " / " + cpu + " / " + gpu +
                                   ". Only FRI-HXX M1030 BIOS 1.15 with Ryzen 5 7640HS and Radeon 760M is supported."
            };
        }
    }
}
