using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    internal class EasyProtocolDeviceInfo : EasyProtocol
    {
        public EasyProtocolDeviceInfo()
            : base(0, 32)
        {
            SendUInt16(0);
            SendUInt16(1);
            SendUInt32(0xFF000000);
            SendUInt32(0x32);
        }

        public EasyProtocolDeviceInfo(byte frameVersion)
            : base(frameVersion, 32)
        {
            SendUInt16(0);
            SendUInt16(1);
            SendUInt32(0xFF000000);
            SendUInt32(0x32);
        }
    }
}
