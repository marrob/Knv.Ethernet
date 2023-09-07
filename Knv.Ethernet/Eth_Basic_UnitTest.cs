
namespace Knv.Eth
{

    using NUnit.Framework;
    using SharpPcap;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.NetworkInformation;

    
   // https://github.com/dotpcap/sharppcap
    
    [TestFixture]
    internal class Eth_Basic_UnitTest
    {

        const int MacAddrLen = 6;
        string LOG_ROOT_DIR = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        [Test]
        public void SampleRrequesResponse()
        {
            /*
             * 
             * Egy ethernet csomag felépítése
             * Destination MAC - DUT TTC580 - 6bájt
             * Source MAC - PC NIC RTL8139D - 6bájt
             * Type: ARP protokol 0x08, 0x06 - 2bájt
             * Pyaload: 0x55, 0x55, 0x55, 0x55 - 4bájt ennek az inverzét várjuk a válaszban. 
             * 
             */
            byte[] txPacket = new byte[]
            {
                0x88, 0x23, 0xFE, 0x02, 0x78, 0xB4, // Destination MAC (DUT TTC580) 
                0x00, 0x40, 0xF4, 0x9C, 0xA5, 0xE4, // Source MAC (PC NIC RTL8139D)
                0x08, 0x06,                         // Type: ARP
                0x55, 0x55, 0x55, 0x55              // Payload
            };

            /*
             * Az elkdött üzenet után várom az Ethernet packet-et
             * A felépítése:
             * 
             *  Destination MAC - PC NIC RTL8139D - 6 bájt - MacAddrLen
             *  Source MAC - DUT TTC580 - 6bájt
             *  Type - 2 bájt
             *  Payload - 4bájt
             */
            byte[] rxPacket = new byte[]
            {
                0x00, 0x40, 0xF4, 0x9C, 0xA5, 0xE4, // Destination MAC (DUT TTC580) 
                0x88, 0x23, 0xFE, 0x02, 0x78, 0xB4, // Source MAC (PC NIC RTL8139D)
                0x08, 0x06,                         // Type: ARP
                0xAA, 0xAA, 0xAA, 0xAA              // Payload
            };
        }




        [Test]
        public void Basics()
        {
            Assert.IsTrue(PhysicalAddress.Parse("0040F49CA5E4").Equals(PhysicalAddress.Parse("0040F49CA5E4")));
            Assert.IsTrue(new PhysicalAddress(new byte[] { 0x00, 0x40, 0xF4, 0x9C, 0xA5, 0xE4 }).Equals(new PhysicalAddress(new byte[] { 0x00, 0x40, 0xF4, 0x9C, 0xA5, 0xE4 })));
        }

        [Test]
        public void SharpPcapVersion()
        {
            var ver = Pcap.SharpPcapVersion;
            Assert.AreEqual("6.2.5.0", ver.ToString());
        }

        [Test]
        public void SearchNetworkInterfaceByMAC()
        {
            var devices = CaptureDeviceList.Instance;

            /*PC NIC RTL8139D: 00:40:F4:9C:A5:E4 -> 0x00, 0x40, 0xF4, 0x9C, 0xA5, 0xE4 -> 0040F49CA5E4 */
            var device = devices.First(n =>
            {
                if (n.MacAddress != null)
                    return n.MacAddress.Equals(PhysicalAddress.Parse("0040F49CA5E4"));
                else
                    return false;
            });
        }


        [Test]
        public void SendPacket()
        {

            var devices = CaptureDeviceList.Instance;

            /*PC NIC RTL8139D: 00:40:F4:9C:A5:E4 -> 0x00, 0x40, 0xF4, 0x9C, 0xA5, 0xE4 -> 0040F49CA5E4 */
            var device = devices.First(n =>
            {
                if (n.MacAddress != null)
                    return n.MacAddress.Equals(PhysicalAddress.Parse("0040F49CA5E4"));
                else
                    return false;
            });

            Debug.WriteLine(device.Description);


  

            device.Open(DeviceModes.Promiscuous,1000);

            /*
             * 
             * Egy ethernet csomag felépítése
             * Destination MAC - DUT TTC580 - 6bájt
             * Source MAC - PC NIC RTL8139D - 6bájt
             * Type: ARP protokol 0x08, 0x06 - 2bájt
             * Pyaload: 0x55, 0x55, 0x55, 0x55 - 4bájt ennek az inverzét várjuk a válaszban. 
             * 
             */
            byte[] txPacket = new byte[]
            {
                0x88, 0x23, 0xFE, 0x02, 0x78, 0xB4, // Destination MAC (DUT TTC580) 
                0x00, 0x40, 0xF4, 0x9C, 0xA5, 0xE4, // Source MAC (PC NIC RTL8139D)
                0x08, 0x06,                         // Type: ARP
                0x55, 0x55, 0x55, 0x55              // Payload
            };
            device.SendPacket(txPacket);


            /*
             * Az elkdött üzenet után várom az Ethernet packet-et
             * A felépítése:
             * 
             *  Destination MAC - PC NIC RTL8139D - 6 bájt - MacAddrLen
             *  Source MAC - DUT TTC580 - 6bájt
             *  Type - 2 bájt
             *  Payload - 4bájt
             * 
             */
            Console.WriteLine("Started");
            long startTick = DateTime.Now.Ticks;
            long timeoutMs = 4000;
            var destMacAddrBuff = new byte[MacAddrLen]; //Ez lesz a PC NIC RTL8139D
            var srcMacAddrBuff = new byte[MacAddrLen];  //Ez lesz a DUT TTC580
    
            do
            {
                if (device.GetNextPacket(out PacketCapture rxPacket) == GetPacketStatus.PacketRead)
                {
                    var payload = rxPacket.Data.ToArray();
                    Buffer.BlockCopy(payload, 0, destMacAddrBuff, 0, MacAddrLen);
                    Buffer.BlockCopy(payload, MacAddrLen, srcMacAddrBuff, 0, MacAddrLen);
                    Console.WriteLine("Packet: " + string.Join(" ", payload.Select(x => x.ToString("X2"))));
                

                    if (new PhysicalAddress(srcMacAddrBuff).Equals(PhysicalAddress.Parse("8823FE0278B4")))
                    {
                        Console.WriteLine("Response Packet: " + string.Join(" ", payload.Select(x => x.ToString("X2"))));
                    }
                }

                if (DateTime.Now.Ticks - startTick > timeoutMs * 10000)
                {
                    break;
                }

            } while (true);


            device.Close();
        }


    }
}
