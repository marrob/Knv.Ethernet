﻿

namespace Knv.Ethernet
{
    using SharpPcap;
    using System;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Reflection;

    public class EthernetPacketTool: IDisposable
    {
        const int MacAddrLen = 6;
        const int EthPacketTypeLen = 2;
        static readonly byte[] EthPacketType = new byte[] { 0x08, 0x00 };

        ILiveDevice _device = null;
        bool _disposed = false;
        string _srcMacAddr = string.Empty;


      
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcMacAddr">eg:0040F49CA5E4</param>
        /// <param name="simulation"></param>
        public EthernetPacketTool(string srcMacAddr)
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
                Log.LogWriteLine(msg);
                throw new Exception(msg);
            }

            _device.Open();
            Log.LogWriteLine($"Start, Source MAC:{srcMacAddr}, Version: {Version()}, SharpPcap: {Pcap.SharpPcapVersion}");
            /*
            _device.Filter = $"ether src or dst {_srcMacAddr}";
            LogWriteLine($"Capture Filter: {_device.Filter}");
            */
        }
        public string SendAndCheckResponse(string destMacAddr, byte[] dataToSend, byte[] expectedDataToReceive, int timeoutMs = 3000)
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
             *     0x08, 0x00,                         // Type: ?
             *     0x55, 0x55, 0x55, 0x55              // Payload - DataToSend
             * };
             */

            var txEthPacket = new byte[2 * MacAddrLen + EthPacketTypeLen + dataToSend.Length];
            Buffer.BlockCopy(destMac, 0, txEthPacket, 0, MacAddrLen);
            Buffer.BlockCopy(srcMac, 0, txEthPacket, MacAddrLen, MacAddrLen);
            Buffer.BlockCopy(EthPacketType, 0, txEthPacket, 2 * MacAddrLen, EthPacketType.Length);
            Buffer.BlockCopy(dataToSend, 0, txEthPacket, 2 * MacAddrLen + EthPacketType.Length, dataToSend.Length);
            _device.SendPacket(txEthPacket);
            Log.LogWriteLine($"Tx:{string.Join(" ", txEthPacket.Select(x => x.ToString("X2")))}");


            /*
             * Ez Capture Filter és Nem Display Filter (ha ez be van állítva akkor ez hatással van a WireShark-ra is)
             * https://wiki.wireshark.org/CaptureFilters#useful-filters
             * https://www.wireshark.org/docs/man-pages/pcap-filter.html
             * 
             *
             */
         


            /*
             * A válasz üzenet felépítése:
             * Example:
             * byte[] rxPacket = new byte[]
             * {
             *   0x00, 0x40, 0xF4, 0x9C, 0xA5, 0xE4, // Destination MAC (DUT TTC580) 
             *   0x88, 0x23, 0xFE, 0x02, 0x78, 0xB4, // Source MAC (PC NIC RTL8139D)
             *   0x08, 0x00,                         // Type: ?
             *   0xAA, 0xAA, 0xAA, 0xAA              // Payload - Expected DataToRecieve
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
                    Log.LogWriteLine($"Rx:{string.Join(" ", respPayload.Select(x => x.ToString("X2")))}");

                    if (new PhysicalAddress(respSrcMac).Equals(PhysicalAddress.Parse(destMacAddr)))
                    {//A ha válsz EthPacketben a forrás a cimzett volt, akkor ez a válasz a küldött üzentre

                        if (respPayload.Length >= (2 * MacAddrLen))
                        {
                            var headerLen = 2 * MacAddrLen + EthPacketType.Length;
                            var respData = new byte[respPayload.Length - headerLen];
                            Buffer.BlockCopy(respPayload, headerLen, respData, 0, respData.Length);
                            Log.LogWriteLine("Response: " + string.Join(" ", respData.Select(x => x.ToString("X2"))));

                            //Ha a válsz hosszabb mint a várt adat, levágjuk a fölleges részt.
                            byte[] reSizedRespData = new byte[expectedDataToReceive.Length];
                            if (respData.Length >= expectedDataToReceive.Length)
                                Buffer.BlockCopy(respData, 0, reSizedRespData, 0, expectedDataToReceive.Length);

                            if (Enumerable.SequenceEqual(reSizedRespData, expectedDataToReceive))
                                return "Passed";
                        }
                    }
                }

                if (DateTime.Now.Ticks - startTick > timeoutMs * 10000)
                {
                    Log.LogWriteLine($"No excepted data received from destination in {timeoutMs}ms. Destination MAC:{destMacAddr}");
                    return "Failed";
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

