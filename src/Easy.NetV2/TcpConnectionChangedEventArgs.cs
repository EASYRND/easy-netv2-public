using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    public enum EasyTcpConnectionType
    {
        Disconnected,
        Connected
    }

    public class EasyTcpConnectionChangedEventArgs : EventArgs
    {
        public EasyTcpConnectionType Type { get; }

        public EasyTcpConnectionChangedEventArgs(EasyTcpConnectionType type)
        {
            Type = type;
        }
    }
}
