using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;

// ACPI/WMI firmware surface used by the FRI-H56 build: charge thresholds,
// Smart/High performance mode, F1-F12 row mode and read-only sensors.
internal static class HonorFirmware
{
    [StructLayout(LayoutKind.Sequential)]
    private struct PowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte Reserved;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out PowerStatus status);

    private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

    public static bool Handles(string command)
    {
        return command == "platform-status" ||
               command.StartsWith("firmware-", StringComparison.Ordinal) ||
               command.StartsWith("charge-", StringComparison.Ordinal) ||
               command.StartsWith("keyboard-", StringComparison.Ordinal);
    }

    private sealed class Session : IDisposable
    {
        private readonly Mutex mutex;
        private readonly ManagementObject instance;
        private bool held;

        public Session()
        {
            mutex = new Mutex(false, @"Local\HonorStandalone.Firmware");
            try { held = mutex.WaitOne(10000); }
            catch (AbandonedMutexException) { held = true; }
            if (!held) { mutex.Dispose(); throw new TimeoutException("Another firmware operation is in progress."); }

            var scope = new ManagementScope(@"\\.\root\wmi");
            scope.Options.EnablePrivileges = true;
            scope.Connect();
            using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM OemWMIMethod")))
            using (var all = searcher.Get())
            {
                var matches = all.Cast<ManagementObject>()
                    .Where(x => (Convert.ToString(x["InstanceName"]) ?? "").EndsWith("HWMI_0", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (matches.Length != 1) throw new InvalidOperationException("Expected exactly one ACPI HWMI_0 instance.");
                instance = matches[0];
            }
        }

        public byte[] Call(byte main, byte sub, byte index, byte parameter = 0)
        {
            using (var input = instance.GetMethodParameters("OemWMIfun"))
            {
                byte[] bytes = new byte[64];
                bytes[0] = main;
                bytes[1] = sub;
                bytes[2] = index;
                bytes[3] = parameter;
                input["u8Input"] = bytes;
                using (var reply = instance.InvokeMethod("OemWMIfun", input, null))
                {
                    byte[] data = (byte[])reply["u8Output"];
                    if (data == null || data.Length == 0 || data[0] != 0)
                        throw new InvalidOperationException("固件拒绝了 " + main + "/" + sub + " 请求。");
                    return data;
                }
            }
        }

        public void Dispose()
        {
            if (instance != null) instance.Dispose();
            if (held) { mutex.ReleaseMutex(); held = false; }
            mutex.Dispose();
        }
    }

    private static int Mode(Session session) { return session.Call(4, 14, 0)[1]; }
    private static int KeyboardMode(Session session) { return session.Call(4, 6, 0)[1]; }

    private static string ModeName(int code)
    {
        if (code == 0) return "Smart";
        if (code == 1) return "Performance";
        if (code == 3) return "HUNTER";
        return "Unknown";
    }

    private static int Word(byte[] data, int index) { return data[index] | (data[index + 1] << 8); }

    private static byte[] TryCall(Session session, byte main, byte sub, byte index)
    {
        try { return session.Call(main, sub, index); }
        catch { return null; }
    }

    private static int PerformanceOwnerCount()
    {
        return Process.GetProcessesByName("HnPerformanceCenter").Length +
               Process.GetProcessesByName("HnPerfPowerNexus").Length;
    }

    private static string PowerText(bool ac, int percent, bool charging)
    {
        if (!ac) return "电池供电 · " + percent + "%";
        return charging ? "已接电源 · " + percent + "% · 充电中" : "已接电源 · " + percent + "%";
    }

    private static void CheckPower(Session session, bool requireAc)
    {
        ChargeSnapshot(session);
        if (!requireAc) return;
        PowerStatus status;
        if (!GetSystemPowerStatus(out status))
            throw new InvalidOperationException("无法读取系统电源状态。");
        if (status.ACLineStatus != 1)
            throw new InvalidOperationException("交流电源未连接，固件写入已拒绝。");
        if (status.BatteryLifePercent < 20)
            throw new InvalidOperationException("电池电量低于 20%，固件写入已拒绝。");
    }

    private static object ChargeSnapshot(Session session)
    {
        byte[] current = session.Call(3, 17, 0);
        int start = current[1];
        int stop = current[2];
        PowerStatus status;
        if (!GetSystemPowerStatus(out status))
            throw new InvalidOperationException("无法读取系统电源状态。");
        bool ac = status.ACLineStatus == 1;
        bool charging = status.BatteryFlag != 8 && (status.BatteryFlag & 2) != 0;
        int percent = status.BatteryLifePercent;
        return new
        {
            ChargeStartPercent = start,
            ChargeStopPercent = stop,
            AcOnline = ac,
            BatteryPercent = percent,
            Charging = charging,
            ChargeSupported = true,
            StatusText = PowerText(ac, percent, charging)
        };
    }

    private static void SetCharge(Session session, int start, int stop)
    {
        if (start < 0 || start > 99) throw new ArgumentOutOfRangeException("start", "开始充电值必须在 0–99% 之间。");
        if (stop < 1 || stop > 100) throw new ArgumentOutOfRangeException("stop", "停止充电值必须在 1–100% 之间。");
        if (start >= stop) throw new ArgumentException("开始充电值必须小于停止充电值。");

        byte[] before = session.Call(3, 17, 0);
        int oldStart = before[1];
        int oldStop = before[2];
        try
        {
            session.Call(3, 16, (byte)start, (byte)stop);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Thread.Sleep(100);
                byte[] now = session.Call(3, 17, 0);
                if (now[1] == start && now[2] == stop) return;
            }
            throw new InvalidOperationException("充电阈值写入后未能在固件中读回一致的值。");
        }
        catch
        {
            try { session.Call(3, 16, (byte)oldStart, (byte)oldStop); } catch { }
            throw;
        }
    }

    private static void SetMode(Session session, int target)
    {
        if (target != 0 && target != 1)
            throw new NotSupportedException("FRI-HXX 仅支持智能和高能模式。");
        if (PerformanceOwnerCount() != 0)
            throw new InvalidOperationException("荣耀电脑管家性能服务仍在运行，请先退出电脑管家后重试。");
        CheckPower(session, true);

        int before = Mode(session);
        try
        {
            session.Call(4, 15, (byte)target);
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Thread.Sleep(250);
                if (Mode(session) == target) return;
            }
            throw new InvalidOperationException("性能模式写入后未能在固件中读回一致的值。");
        }
        catch
        {
            try { session.Call(4, 15, (byte)before); } catch { }
            throw;
        }
    }

    private static void SetKeyboardMode(Session session, int target)
    {
        if (target != 1 && target != 2) throw new ArgumentOutOfRangeException("target");
        int before = KeyboardMode(session);
        try
        {
            session.Call(4, 7, (byte)target);
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Thread.Sleep(80);
                if (KeyboardMode(session) == target) return;
            }
            throw new InvalidOperationException("Fn 行模式写入后未能读回一致的值。");
        }
        catch
        {
            try { session.Call(4, 7, (byte)before); } catch { }
            throw;
        }
    }

    private static object Snapshot(Session session)
    {
        int mode = Mode(session);
        byte[] temp = TryCall(session, 2, 2, 0);
        byte[] fan = TryCall(session, 2, 8, 0);
        byte[] volts = TryCall(session, 2, 9, 2);
        int celsius = temp != null && temp.Length > 2 ? temp[2] : 0;
        int fan0 = fan != null && fan.Length > 2 ? Word(fan, 1) : 0;
        int millivolts = volts != null && volts.Length > 2 ? Word(volts, 1) : 0;
        return new
        {
            Profile = PlatformProfile.Current.Name,
            ModeCode = mode,
            Mode = ModeName(mode),
            ModeName = ModeName(mode),
            ModeWrite = PlatformProfile.Current.ModeWrite,
            TurboXAvailable = PlatformProfile.Current.ModeWrite && PerformanceOwnerCount() == 0,
            CpuCelsius = celsius,
            Fan0ActualRpm = fan0,
            Fan1ActualRpm = 0,
            PdAdapterWatts = millivolts / 1000.0,
            AcOnline = false,
            BatteryPercent = 0,
            Charging = false,
            HonorPerformanceHosts = PerformanceOwnerCount()
        };
    }

    public static void Execute(string command)
    {
        if (command == "platform-status")
        {
            Console.WriteLine(Json.Serialize(PlatformProfile.Current.Snapshot()));
            return;
        }

        using (var session = new Session())
        {
            switch (command)
            {
                case "firmware-status":
                {
                    var status = Snapshot(session);
                    Console.WriteLine(Json.Serialize(status));
                    return;
                }
                case "firmware-policy":
                {
                    var charge = ChargeSnapshot(session);
                    Console.WriteLine(Json.Serialize(new { DttSupported = false, ChargeStart = 0, ChargeStop = 0, Charge = charge }));
                    return;
                }
                case "firmware-smart": SetMode(session, 0); Console.WriteLine(Json.Serialize(new { RequestedMode = "Smart", Verified = true })); return;
                case "firmware-performance": SetMode(session, 1); Console.WriteLine(Json.Serialize(new { RequestedMode = "Performance", Verified = true })); return;
                case "charge-status":
                    Console.WriteLine(Json.Serialize(ChargeSnapshot(session)));
                    return;
                case "charge-home": SetCharge(session, 40, 70); Console.WriteLine(Json.Serialize(ChargeSnapshot(session))); return;
                case "charge-office": SetCharge(session, 70, 90); Console.WriteLine(Json.Serialize(ChargeSnapshot(session))); return;
                case "charge-travel": SetCharge(session, 95, 100); Console.WriteLine(Json.Serialize(ChargeSnapshot(session))); return;
                case "charge-off": SetCharge(session, 0, 100); Console.WriteLine(Json.Serialize(ChargeSnapshot(session))); return;
                case "keyboard-status":
                case "keyboard-icons":
                case "keyboard-fkeys":
                {
                    if (command == "keyboard-icons") SetKeyboardMode(session, 1);
                    if (command == "keyboard-fkeys") SetKeyboardMode(session, 2);
                    int code = KeyboardMode(session);
                    Console.WriteLine(Json.Serialize(new { FnStateCode = code, StandardFKeys = code == 2 }));
                    return;
                }
            }

            if (command.StartsWith("charge-custom:", StringComparison.Ordinal))
            {
                string[] parts = command.Split(':');
                int start;
                int stop;
                if (parts.Length != 3 || !Int32.TryParse(parts[1], out start) || !Int32.TryParse(parts[2], out stop))
                    throw new ArgumentException("自定义充电阈值格式无效。");
                if (start < 20 || start > 99 || stop < 21 || stop > 100 || start >= stop)
                    throw new ArgumentException("充电阈值必须满足：开始值 20–99%，停止值 21–100%，且开始值小于停止值。");
                SetCharge(session, start, stop);
                Console.WriteLine(Json.Serialize(ChargeSnapshot(session)));
                return;
            }
        }

        throw new NotSupportedException("不支持的控制操作：" + command);
    }
}
