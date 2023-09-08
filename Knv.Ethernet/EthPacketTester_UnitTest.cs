
namespace Knv.Eth
{
    using Knv.Ethernet;
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
    using System.Reflection;

    [TestFixture]
    internal class EthPacketTester_UnitTest
    {
        string LOG_ROOT_DIR = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        /*
        string SRC_MAC_ADDR = "0040F49CA5E4";        //PC
        string DEST_MAC_ADDR = "8823FE0278B4";      //DUT
        */

        string SRC_MAC_ADDR = "00802F36770E";        //PC - ECUTS2
        string DEST_MAC_ADDR = "8823FE0278B4";      //DUT

        [Test]
        public void EthPacketTester_Test()
        {
            using (var ept = new EthPacketTester(SRC_MAC_ADDR))
            {
        

                ept.OpenLogByNpp(ept.LogSave(LOG_ROOT_DIR, MethodBase.GetCurrentMethod().Name));
            }
        }


        [Test]
        public void EthernetTester()
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = $@"{TestContext.CurrentContext.TestDirectory}\Knv.Ethernet.exe";
            startInfo.Arguments = "00E04C3DA62F 8823FE0278B4 \"D:\\Log\" 1694170903";

            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }


        }

    }
}
