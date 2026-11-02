using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    internal class EasyDeviceInfo
    {
        public string Name { get; set; }
        public double FirmwareVersion { get; set; }
        public EasyMacAddress MacAddress { get; set; }
        public EasyIPAddress RemoteIP { get; set; }
        public uint Options { get; set; }
        public string SerialNumber { get; set; }
        public string UserData { get; set; }

        public ushort CheckSum { get; set; }
        public string DisplayName
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Name);
                sb.Append(" (");
                sb.Append(SerialNumber);
                if (string.IsNullOrEmpty(UserData) == false)
                {
                    sb.Append(", ");
                    sb.Append(UserData);
                }
                sb.Append(")");
                return sb.ToString();
            }
        }
    }
}
