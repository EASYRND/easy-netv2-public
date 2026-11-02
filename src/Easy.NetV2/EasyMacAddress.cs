using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    [DebuggerDisplay("{Address}")]
    public class EasyMacAddress : IEquatable<EasyMacAddress>
    {
        public static readonly EasyMacAddress Default = new EasyMacAddress("00:45:52:" + DateTime.Now.ToString("yy") + ":00:00");
        public static readonly EasyMacAddress Zero = new EasyMacAddress(0);

        private ulong mac;

        public byte[] Bytes
        {
            get => BitConverter.GetBytes(mac).Take(6).Reverse().ToArray();
        }

        public string Address => BitConverter.ToString(Bytes);
        internal ulong Value => this.mac;

        public EasyMacAddress(ulong mac)
        {
            this.mac = mac;
        }

        public EasyMacAddress(string address)
            : this(address.Split('-', ':').Select(x => byte.Parse(x, System.Globalization.NumberStyles.HexNumber)).Reverse().ToArray())
        {

        }

        public EasyMacAddress(byte[] bytes)
        {
            var temp = new byte[8];
            Array.Copy(bytes, temp, Math.Min(bytes.Length, 6));
            mac = BitConverter.ToUInt64(temp, 0);
        }

        public bool Equals(EasyMacAddress other)
        {
            return mac == other.mac;
        }

        public override string ToString()
        {
            return Address;
        }

        public static EasyMacAddress operator +(EasyMacAddress a, int b)
        {
            return new EasyMacAddress(a.mac + (ulong)b);
        }

        public static EasyMacAddress operator -(EasyMacAddress a, int b)
        {
            return new EasyMacAddress(a.mac - (ulong)b);
        }
    }
}
