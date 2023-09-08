
namespace Knv.Ethernet
{
    using System;
    using System.Linq;

    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || args.Length != 4)
            {
                Console.WriteLine("Error Invlaid Argumants!");
                Console.WriteLine("Arguments: <Source MAC> <DUT MAC> <Log Directory> <UTC Timestamp>");
                Console.WriteLine(@"Example:00E04C3DA62F 8823FE0278B4 ""D:\Log\Valami Logj"" 1694170903");
                
                foreach (var arg in args)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(arg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                
                goto EndWithError;
            }

            string srcMac = args[0].ToUpper().Trim();
            if (srcMac.Length != 12)
            {
                Console.WriteLine($"Error: Source MAC {srcMac} format invalid.");
                goto EndWithError;
            }

            string destMac = args[1].ToUpper().Trim();
            if (destMac.Length != 12)
            {
                Console.WriteLine($"Error: Source MAC {srcMac} format invalid.");
                goto EndWithError;
            }

            string logDirectory = args[2].ToUpper().Trim();
            long utcTimestamp = Convert.ToInt64(args[3].Trim());

            Console.WriteLine("Arguments:");
            Console.WriteLine($"Source MAC:{srcMac}");
            Console.WriteLine($"DUT MAC:{destMac}");
            Console.WriteLine($"Log Directory:{logDirectory}");
            Console.WriteLine($"UTC Timestamp:{utcTimestamp}");

            string testResult = "Unknown";
            var reqData = new byte[] { 0xAA, 0x55, 0xAA, 0x55 };
            var expData = new byte[] { 0x55, 0xAA, 0x55, 0xAA };
            var result = new byte[expData.Length];
            var maxRepeat = 7;

            using (var ept = new EthernetPacketTool(srcMac))
            {
                try
                {
                    ept.LogWriteLine($"*** Src:{srcMac} Dest:{destMac}, Data:{string.Join(" ", reqData.Select(x => x.ToString("X2")))} ***");
                    for (int repeat = 0; repeat < maxRepeat; repeat++)
                    {
                        var respData = ept.SendReceive(destMac, reqData, 1000);
                        if (respData.Length >= reqData.Length)
                            Array.Copy(respData, result, result.Length);

                        if (Enumerable.SequenceEqual(result, expData))
                        {
                            testResult = "Passed";
                            break;
                        }
                        else
                        {
                            testResult = "Failed";
                            ept.LogWriteLine($"Repeat {repeat}/{maxRepeat - 1}");
                        }

                        System.Threading.Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    var msg = $"Error: {ex}";
                    ept.LogWriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    goto EndWithError;
                }
                finally
                {
                    ept.LogWriteLine($"Test Result: {testResult}");
                    ept.LogSave(logDirectory, "_Ethernet", utcTimestamp);

                    if (testResult == "Passed")
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(testResult);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(testResult);
                    }
                }
            }
            Console.ReadLine();
            return;

            EndWithError:
 

            Console.ReadLine();

        }
    }
}
