using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

// Windows gamma-ramp display adjustment for machines whose panel does not expose
// a factory 3D LUT (the FRI-HXX / AMD Radeon 760M case). It never touches the
// Intel ControlLib path used by the DRA-XX build.
internal static class DisplayGamma
{
    private const int RampEntries = 256;
    private const int RampLength = RampEntries * 3;
    private const int GammaRampSize = RampLength * 2;
    private const double RhythmPeriodSeconds = 60;
    private const double RhythmAmplitude = 0.04;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string device, uint deviceNumber, ref DisplayDevice display, uint flags);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateDC(string driver, string device, string output, IntPtr initialData);
    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr dc);
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr window, IntPtr dc);
    [DllImport("gdi32.dll")]
    private static extern bool GetDeviceGammaRamp(IntPtr dc, ushort[] ramp);
    [DllImport("gdi32.dll")]
    private static extern bool SetDeviceGammaRamp(IntPtr dc, ushort[] ramp);

    private static string _state;
    private static string DefaultState { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HonorControl", "DeviceState"); } }
    private static string FallbackState { get { return Path.Combine(AppContext.BaseDirectory, "DeviceState"); } }
    private static string State
    {
        get
        {
            if (_state != null) return _state;
            try
            {
                Directory.CreateDirectory(DefaultState);
                string probe = Path.Combine(DefaultState, ".write-probe");
                File.WriteAllText(probe, "");
                File.Delete(probe);
                _state = DefaultState;
            }
            catch (UnauthorizedAccessException) { Directory.CreateDirectory(FallbackState); _state = FallbackState; }
            return _state;
        }
    }
    private static string SettingsPath { get { return Path.Combine(State, "display-gamma.txt"); } }
    private static string BackupPath { get { return Path.Combine(State, "display-gamma-backup.txt"); } }
    private static string RhythmPath { get { return Path.Combine(State, "display-gamma-rhythm.txt"); } }

    private static bool RhythmActive { get { return File.Exists(RhythmPath); } }

    private static double RhythmBrightness()
    {
        if (!RhythmActive) return 1.0;
        long ticks;
        if (!long.TryParse(File.ReadAllText(RhythmPath).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks)) return 1.0;
        double seconds;
        try { seconds = (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds; }
        catch (ArgumentOutOfRangeException) { return 1.0; }
        if (seconds < 0) seconds = 0;
        double time = seconds % RhythmPeriodSeconds;
        double phase = time < 30 ? 0 : time < 40 ? (time - 30) / 10 : (60 - time) / 20;
        double rise = (1 - Math.Cos(phase * Math.PI)) / 2;
        return 1 + RhythmAmplitude * rise;
    }

    private static IntPtr DeviceContext(out int displayCount)
    {
        displayCount = 0;
        uint index = 0;
        while (true)
        {
            var device = new DisplayDevice();
            device.cb = Marshal.SizeOf(typeof(DisplayDevice));
            if (!EnumDisplayDevices(null, index, ref device, 0)) break;
            displayCount++;
            const int attached = 0x1;
            const int mirror = 0x8;
            if ((device.StateFlags & attached) == 0 || (device.StateFlags & mirror) != 0) { index++; continue; }
            IntPtr deviceContext = CreateDC(null, device.DeviceName, null, IntPtr.Zero);
            if (deviceContext != IntPtr.Zero) return deviceContext;
            index++;
        }
        return GetDC(IntPtr.Zero);
    }

    private static void Close(IntPtr deviceContext)
    {
        if (deviceContext == IntPtr.Zero) return;
        if (!DeleteDC(deviceContext)) ReleaseDC(IntPtr.Zero, deviceContext);
    }

    private static ushort[] Identity()
    {
        var ramp = new ushort[RampLength];
        for (int i = 0; i < RampEntries; i++)
        {
            ushort value = (ushort)(i * 257);
            ramp[i] = value;
            ramp[RampEntries + i] = value;
            ramp[2 * RampEntries + i] = value;
        }
        return ramp;
    }

    private static ushort[] Build(DisplayEffects effects, double brightness)
    {
        double[] factors = effects.GammaFactors(DateTime.Now.Hour);
        var ramp = new ushort[RampLength];
        for (int i = 0; i < RampEntries; i++)
        {
            int red = (int)Math.Round(i * 257 * factors[0] * brightness);
            int green = (int)Math.Round(i * 257 * factors[1] * brightness);
            int blue = (int)Math.Round(i * 257 * factors[2] * brightness);
            ramp[i] = (ushort)Math.Min(65535, Math.Max(0, red));
            ramp[RampEntries + i] = (ushort)Math.Min(65535, Math.Max(0, green));
            ramp[2 * RampEntries + i] = (ushort)Math.Min(65535, Math.Max(0, blue));
        }
        return ramp;
    }

    private static bool Same(ushort[] left, ushort[] right)
    {
        return left.Length == right.Length && left.SequenceEqual(right);
    }

    private static void SaveBackup(ushort[] ramp)
    {
        Directory.CreateDirectory(State);
        var bytes = new byte[GammaRampSize];
        Buffer.BlockCopy(ramp, 0, bytes, 0, bytes.Length);
        File.WriteAllText(BackupPath, Convert.ToBase64String(bytes));
    }

    private static ushort[] ReadBackup()
    {
        if (!File.Exists(BackupPath)) return null;
        byte[] bytes = Convert.FromBase64String(File.ReadAllText(BackupPath).Trim());
        if (bytes.Length != GammaRampSize) throw new InvalidDataException("显示伽马备份大小不正确。");
        var ramp = new ushort[RampLength];
        Buffer.BlockCopy(bytes, 0, ramp, 0, bytes.Length);
        return ramp;
    }

    private static DisplayEffects ReadEffects()
    {
        if (!File.Exists(SettingsPath)) return new DisplayEffects("off", 0);
        string[] parts = File.ReadAllText(SettingsPath).Trim().Split(' ');
        int temperature;
        if (parts.Length != 2 || !int.TryParse(parts[1], out temperature)) throw new InvalidDataException("显示伽马设置无效。");
        return new DisplayEffects(parts[0], temperature);
    }

    public static object Execute(string command, string[] args)
    {
        if (command == "display-gamma-status") return Status();
        if (command == "display-gamma-restore") return Restore();
        if (command == "display-gamma-rhythm-start") return RhythmStart();
        if (command == "display-gamma-rhythm-stop") return RhythmStop();
        if (command == "display-gamma-rhythm-tick") return RhythmTick();
        if (command == "display-gamma-apply")
        {
            int temperature;
            if (args.Length != 3 || !int.TryParse(args[2], out temperature)) throw new ArgumentException("Expected an effect and temperature.");
            return Apply(args[1], temperature);
        }
        throw new ArgumentException("Unknown display gamma command.");
    }

    private static object Result(int count, DisplayEffects effects, bool verified)
    {
        return new
        {
            Backend = "Windows GDI gamma ramp",
            DisplayCount = count,
            Active = effects.Active || RhythmActive,
            EffectStyle = effects.Style,
            Temperature = effects.Temperature,
            RhythmActive = RhythmActive,
            RhythmBrightness = Math.Round(RhythmBrightness(), 3),
            EffectsVerified = verified,
            SamplesPerChannel = RampEntries
        };
    }

    private static object Status()
    {
        DisplayEffects effects = ReadEffects();
        int count;
        IntPtr deviceContext = DeviceContext(out count);
        try
        {
            var current = new ushort[RampLength];
            bool readable = GetDeviceGammaRamp(deviceContext, current);
            bool verified = readable && Same(current, Build(effects, RhythmBrightness()));
            return Result(count, effects, verified);
        }
        finally { Close(deviceContext); }
    }

    private static object Apply(string style, int temperature)
    {
        return ApplyEffects(new DisplayEffects(style, temperature), true);
    }

    private static object ApplyEffects(DisplayEffects effects, bool saveSettings)
    {
        int count;
        IntPtr deviceContext = DeviceContext(out count);
        try
        {
            var before = new ushort[RampLength];
            if (!GetDeviceGammaRamp(deviceContext, before)) throw new InvalidOperationException("无法读取当前显示伽马表。");
            if (!File.Exists(BackupPath)) SaveBackup(before);
            var wanted = Build(effects, RhythmBrightness());
            if (!SetDeviceGammaRamp(deviceContext, wanted)) throw new InvalidOperationException("系统拒绝了显示伽马调整。");
            var after = new ushort[RampLength];
            if (!GetDeviceGammaRamp(deviceContext, after) || !Same(after, wanted)) throw new InvalidOperationException("显示伽马调整未能读回。");
            if (saveSettings)
            {
                Directory.CreateDirectory(State);
                File.WriteAllText(SettingsPath, effects.Style + " " + effects.Temperature.ToString(CultureInfo.InvariantCulture));
            }
            return Result(count, effects, true);
        }
        finally { Close(deviceContext); }
    }

    private static object RhythmStart()
    {
        Directory.CreateDirectory(State);
        if (!RhythmActive) File.WriteAllText(RhythmPath, DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));
        return ApplyEffects(ReadEffects(), false);
    }

    private static object RhythmStop()
    {
        try { File.Delete(RhythmPath); } catch (FileNotFoundException) { }
        DisplayEffects effects = ReadEffects();
        return effects.Active ? ApplyEffects(effects, false) : Restore();
    }

    private static object RhythmTick()
    {
        return RhythmActive ? ApplyEffects(ReadEffects(), false) : Status();
    }

    private static object Restore()
    {
        int count;
        IntPtr deviceContext = DeviceContext(out count);
        try
        {
            ushort[] saved = ReadBackup();
            ushort[] ramp = saved == null ? Identity() : saved;
            if (!SetDeviceGammaRamp(deviceContext, ramp)) throw new InvalidOperationException("无法恢复原始显示伽马表。");
            try { File.Delete(RhythmPath); } catch (FileNotFoundException) { }
            try { File.Delete(SettingsPath); } catch (FileNotFoundException) { }
            try { File.Delete(BackupPath); } catch (FileNotFoundException) { }
            return Result(count, new DisplayEffects("off", 0), true);
        }
        finally { Close(deviceContext); }
    }
}
