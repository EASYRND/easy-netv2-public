using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    public class Address
    {
        public static uint Channel(uint address, int ch, uint offset = 0x10000)
        {
            return address + (uint)(offset * ch);
        }
    }
}
