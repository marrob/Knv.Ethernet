

namespace Knv.Ethernet
{
    using SharpPcap;
    using System;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Reflection;

    public class EthPacketTester : Log, IDisposable
    {
        const int MacAddrLen = 6;
        const int EthPacketTypeLen = 2;
        static readonly byte[] EthPacketType = new byte[] { 0x08, 0x06 };

        ILiveDevice _device = null;
        bool _disposed = false;
        string _srcMacAddr = string.Empty;


      
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcMacAddr">eg:0040F49CA5E4</param>
        /// <param name="simulation"></param>
        public EthPacketTester(string srcMacAddr)
        {
            _srcMacAddr = srcMacAddr;
            //MAC alapján megszerzem az NIC eszközt
            var devices = CaptureDeviceList.Instance;

            try
            {
                _device = devices.First(n =>
                {
                    if (n.MacAddress != null)
                        return n.MacAddress.Equals(PhysicalAddress.Parse(srcMacAddr));
                    else
                        return false;
                });
            }
            catch (Exception ex)
            {
                var msg = $"Error: Your PC does not contain Network Interface (NIC) with {srcMacAddr} MAC adderess. Please check it.\r\n";
                msg += $"Source MAC: {srcMacAddr}, Version: {Version()}, SharpPcap: {Pcap.SharpPcapVersion}\r\n";
                msg += ex.Message;
                LogWriteLine(msg);
                throw new Exception(msg);
            }

            _device.Open();
            LogWriteLine($"Start, Source MAC:{srcMacAddr}, Version: {Version()}, SharpPcap: {Pcap.SharpPcapVersion}");
        }
        public byte[] SendReceive(string destMacAddr, byte[] reqData, int timeoutMs = 3000)
        {

            var destMac = PhysicalAddress.Parse(destMacAddr).GetAddressBytes();
            var srcMac = PhysicalAddress.Parse(_srcMacAddr).GetAddressBytes();

            /*
             * Egy ethernet csomag felépítése
             * Destination MAC - DUT TTC580 - 6bájt
             * Source MAC - PC NIC RTL8139D - 6bájt
             * Type: ARP protokol 0x08, 0x06 - 2bájt
             * Pyaload: 0x55, 0x55, 0x55, 0x55 - 4bájt ennek az inverzét várjuk a válaszban. 
             * 
             * 
             * Example:
             * byte[] txPacket = new byte[]
             * {
             *     0x88, 0x23, 0xFE, 0x02, 0x78, 0xB4, // Destination MAC (DUT TTC580) 
             *     0x00, 0x40, 0xF4, 0x9C, 0xA5, 0xE4, // Source MAC (PC NIC RTL8139D)
             *     0x08, 0x06,                         // Type: ARP
             *     0x55, 0x55, 0x55, 0x55              // Payload
             * };
             */

            var txEthPacket = new byte[2 * MacAddrLen + EthPacketTypeLen + reqData.Length];
            Buffer.BlockCopy(destMac, 0, txEthPacket, 0, MacAddrLen);
            Buffer.BlockCopy(srcMac, 0, txEthPacket, MacAddrLen, MacAddrLen);
            Buffer.BlockCopy(EthPacketType, 0, txEthPacket, 2 * MacAddrLen, EthPacketType.Length);
            Buffer.BlockCopy(reqData, 0, txEthPacket, 2 * MacAddrLen + EthPacketType.Length, reqData.Length);
            _device.SendPacket(txEthPacket);
            LogWriteLine($"Tx:{string.Join(" ", txEthPacket.Select(x => x.ToString("X2")))}");


            /*
             * A válasz üzenet felépítése:
             * Example:
             * byte[] rxPacket = new byte[]
             * {
             *   0x00, 0x40, 0xF4, 0x9C, 0xA5, 0xE4, // Destination MAC (DUT TTC580) 
             *   0x88, 0x23, 0xFE, 0x02, 0x78, 0xB4, // Source MAC (PC NIC RTL8139D)
             *   0x08, 0x06,                         // Type: ARP
             *   0xAA, 0xAA, 0xAA, 0xAA              // Payload
             * };
             */
            long startTick = DateTime.Now.Ticks;
            byte[] respPayload = null;
            var respDestMac = new byte[MacAddrLen]; //Ez lesz a PC NIC RTL8139D
            var respSrcMac = new byte[MacAddrLen];  //Ez lesz a DUT TTC580
            do
            {
                if (_device.GetNextPacket(out PacketCapture rxPacket) == GetPacketStatus.PacketRead)
                {
                    respPayload = rxPacket.Data.ToArray();
                    Buffer.BlockCopy(respPayload, 0, respDestMac, 0, MacAddrLen);
                    Buffer.BlockCopy(respPayload, MacAddrLen, respSrcMac, 0, MacAddrLen);
                    LogWriteLine($"Rx:{string.Join(" ", respPayload.Select(x => x.ToString("X2")))}");

                    if (new PhysicalAddress(respSrcMac).Equals(PhysicalAddress.Parse(destMacAddr)))
                    {//A ha válsz EthPacketben a forrás a cimzett volt, akkor ez a válasz üzenet a krésre

                        if (respPayload.Length >= (2 * MacAddrLen + reqData.Length))
                        {
                            var headerLen = 2 * MacAddrLen + EthPacketType.Length;
                            var respData = new byte[respPayload.Length - headerLen];
                            Buffer.BlockCopy(respPayload, headerLen, respData, 0, respData.Length);
                            LogWriteLine("Response: " + string.Join(" ", respData.Select(x => x.ToString("X2"))));
                            return respData;
                        }
                    }
                }

                if (DateTime.Now.Ticks - startTick > timeoutMs * 10000)
                {
                    LogWriteLine($"No response from destination in {timeoutMs}ms. Destination MAC:{destMacAddr}");
                    return new byte[0];
                }

            } while (true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (_device != null)
                {
                    _device.Close();
                }
            }

            _disposed = true;
        }

        static string Version()
        {
            return ((AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(
                         Assembly.GetExecutingAssembly(), typeof(AssemblyFileVersionAttribute), false)).Version;
        }
    }
}

