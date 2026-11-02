using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    public class EasyProtocolGlobalSetter : EasyProtocol
    {
        public EasyProtocolGlobalSetter() : this(0xFFFFFFFF) { }

        public EasyProtocolGlobalSetter(uint serialNumber)
            : base(1, 32)
        {
            SendUInt16(1);
            SendUInt32(serialNumber);
        }

        public EasyProtocolGlobalSetter(byte frameVersion, uint serialNumber)
            : base(frameVersion, 32)
        {
            SendUInt16(1);
            SendUInt32(serialNumber);
        }

        internal void SendMacAddress(EasyMacAddress macAddress)
        {
            SendUInt32(0xFF00001C);
            SendUInt32(6);
            SendBuffer(macAddress.Bytes.Reverse());
        }

        internal void SendSerialNumber(uint serialNumber)
        {
            SendUInt32(0xFF000000);
            SendUInt32(4);
            SendUInt32(serialNumber);
        }

        public void SendIPAddress(EasyIPAddress ipAddress)
        {
            SendUInt32(0xFF000022);
            SendUInt32(4);
            SendUInt32(ipAddress.Value);
        }

        public void SendUserData(string userData)
        {
            SendUInt32(0xFF000026);
            SendUInt32(8);
            SendString(userData.PadRight(8, ' ').Substring(0, 8));
        }
    }
}
