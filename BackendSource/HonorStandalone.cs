// Honor Control backend for the FRI-H56 build.
// It calls the Microsoft ACPI WMI interface and Windows display APIs directly;
// no HONOR executable, service, DLL, custom driver or registry setting is loaded.
using System;
using System.Text;
using System.Web.Script.Serialization;

internal static class HonorStandalone
{
    private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        string command = args.Length == 0 ? "platform-status" : args[0].ToLowerInvariant();
        if (command == "status") command = "platform-status";

        try
        {
            if (command.StartsWith("display-gamma-", StringComparison.Ordinal))
            {
                if (!PlatformProfile.Current.DisplayGamma)
                    throw new NotSupportedException("Display gamma adjustment is not enabled for this platform.");
                Console.WriteLine(Json.Serialize(DisplayGamma.Execute(command, args)));
                return 0;
            }

            if (HonorFirmware.Handles(command))
            {
                HonorFirmware.Execute(command);
                return 0;
            }

            throw new NotSupportedException("不支持的控制操作：" + command);
        }
        catch (Exception error)
        {
            Console.WriteLine(Json.Serialize(new { Error = error.Message }));
            return 1;
        }
    }
}
