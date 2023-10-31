#define ECUTS2
#define AXAGON
namespace Knv.Eth.UnitTest
{
    
    

    
    using Knv.Ethernet;
    using NUnit.Framework;
    using NUnit.Framework.Internal;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;

    [TestFixture]
    internal class Ethernet_UnitTest
    {
        string LOG_ROOT_DIR = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

#if ECUTS2
        string SRC_MAC_ADDR = "00802F36770E";        //PC
#elif AXAGON
        string SRC_MAC_ADDR = "00E04C3DA62F";        //PC
#endif

        //string DEST_MAC_ADDR = "8823FE0278B4";      //DUT 
        string DEST_MAC_ADDR = "8823FE03A2E0";

        [Test]
        public void EthPacketTester_Test()
        {
            using (var ept = new EthernetPacketTool(SRC_MAC_ADDR))
            {
                for (int repeat = 0; repeat < 8; repeat++)
                {

                    ept.SendAndCheckResponse(DEST_MAC_ADDR, new byte[] { 0x55, 0xAA, 0x55, 0xAA }, new byte[] { 0xAA, 0x55, 0xAA, 0x55 }, 1000);
                    System.Threading.Thread.Sleep(1000);
                }
                ept.OpenLogByNpp(ept.LogSave(LOG_ROOT_DIR, MethodBase.GetCurrentMethod().Name));
            }
        }

        [Test]
        public void EthernetTester()
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = $@"{TestContext.CurrentContext.TestDirectory}\Knv.Ethernet.exe";
            startInfo.Arguments = $"{SRC_MAC_ADDR} {DEST_MAC_ADDR} \"D:\\Log\" _Ethernet 1694170903";

            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
        }

        [Test]
        public void EthernetTester2()
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = $@"{TestContext.CurrentContext.TestDirectory}\Knv.Ethernet.exe";
            startInfo.Arguments = $"{SRC_MAC_ADDR} {DEST_MAC_ADDR}";

            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
        }

    }
}
