using System.Text.Json;
using HonorControl.Shared;

void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

var fri = PlatformCapabilities.ForIdentity("FRI-HXX", "M1030", "1.15", "AMD Ryzen 5 7640HS w/ Radeon 760M Graphics", "AMD Radeon 760M Graphics | Honor Virtual Display Device");
Require(fri.Name == "FRI-HXX", "The FRI-HXX model was not recognized.");
Require(fri.FirmwareWrites, "Firmware writes must be enabled on FRI-HXX.");
Require(fri.ModeWrite, "Performance mode writes must be enabled on FRI-HXX.");
Require(fri.ChargeWrite, "Charge writes must be enabled on FRI-HXX.");
Require(fri.DisplayGamma, "Windows gamma must be enabled on FRI-HXX.");
Require(fri.FnWrite, "Fn row mode writes must be enabled on FRI-HXX.");

var unknown = PlatformCapabilities.ForIdentity("FRI-HXX", "M1030", "1.16", "AMD Ryzen 5 7640HS w/ Radeon 760M Graphics", "AMD Radeon 760M Graphics");
Require(!unknown.FirmwareWrites && unknown.WriteBlockReason.Contains("1.16"), "An unknown BIOS revision was not rejected.");

var other = PlatformCapabilities.ForIdentity("DRA-XX", "M1030", "1.13", "Intel Core Ultra 9 185H", "NVIDIA GeForce RTX 4060");
Require(!other.FirmwareWrites, "An unsupported product must not be writable.");

Console.WriteLine(JsonSerializer.Serialize(new { Verified = true, FriSupported = true, UnknownRejected = true }));
