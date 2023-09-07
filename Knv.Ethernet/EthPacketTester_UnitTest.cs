
namespace Knv.Eth
{
    using Knv.Ethernet;
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Reflection;

    [TestFixture]
    internal class EthPacketTester_UnitTest
    {
        string LOG_ROOT_DIR = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        string SRC_MAC_ADDR = "0040F49CA5E4";        //PC
        string DEST_MAC_ADDR = "8823FE0278B4";      //DUT


        [Test]
        public void EthPacketTester_Test()
        {
            using (var ept = new EthPacketTester(SRC_MAC_ADDR))
            {
                var data = ept.Test(DEST_MAC_ADDR, new byte[] { 0x55, 0x55, 0x55, 0x55 });

                ept.OpenLogByNpp(ept.LogSave(LOG_ROOT_DIR, MethodBase.GetCurrentMethod().Name));
            }
        }

    }
}
