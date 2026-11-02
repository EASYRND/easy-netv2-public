using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    public class EasyProtocolV0 : EasyProtocolGroup, IEasyProtocolCommand
    {
        public static readonly EasyProtocolV0 SaveFlash = new EasyProtocolV0(0xF001);
        public static readonly EasyProtocolV0 LoadFlash = new EasyProtocolV0(0xF002);
        public static readonly EasyProtocolV0 ResetParam = new EasyProtocolV0(0xF000);
        public static readonly EasyProtocolV0 ClearAlarm = new EasyProtocolV0(0x0004);
        public static readonly EasyProtocolV0 ClearCount = new EasyProtocolV0(0x0005);

        public EasyProtocolV0(bool isWrite = false)
            : base(0, isWrite)
        {

        }

        public EasyProtocolV0(ushort command)
            : base(0, command)
        {

        }

        public EasyProtocolV0(byte[] bytes)
            : base(0, bytes)
        {

        }

        public EasyProtocolV0(EasyProtocolV0 other)
            : base(other)
        {

        }
    }
}
