using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    public static class NIC
    {
        public static IPEndPoint[] GetIPAddresses()
        {
            List<IPEndPoint> list = new List<IPEndPoint>(10);
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!nic.SupportsMulticast) continue;
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType != NetworkInterfaceType.Ethernet && nic.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) continue;
                if (nic.IsReceiveOnly) continue;
                foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        list.Add(new IPEndPoint(ip.Address, 0));
                    }
                }
            }

            return list.ToArray();
        }

    }
}
