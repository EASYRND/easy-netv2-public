using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    public class EasyIPAddress
    {
        private uint ip;

        public byte[] Bytes
        {
            get => BitConverter.GetBytes(ip).Reverse().ToArray();
        }

        public uint Value => this.ip;

        public EasyIPAddress(uint ip)
        {
            this.ip = ip;
        }

        public EasyIPAddress(string ip)
        {
            if (IPAddress.TryParse(ip, out IPAddress address))
            {
                this.ip = BitConverter.ToUInt32(address.GetAddressBytes().Reverse().ToArray(), 0);
            }
        }

        public static implicit operator System.Net.IPAddress(EasyIPAddress easyIpAddress)
        {
            return new IPAddress(easyIpAddress.Bytes);
        }

        public override string ToString()
        {
            return ((IPAddress)this).ToString();
        }
    }
}
