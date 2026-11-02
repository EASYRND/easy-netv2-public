using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    public class EasyProtocolV2 : EasyProtocolGroup, IEasyProtocolCommand
    {
        public static readonly EasyProtocolV2 SaveFlash = new EasyProtocolV2(0xF001);
        public static readonly EasyProtocolV2 LoadFlash = new EasyProtocolV2(0xF002);
        public static readonly EasyProtocolV2 ResetParam = new EasyProtocolV2(0xF000);
        public static readonly EasyProtocolV2 ClearAlarm = new EasyProtocolV2(0x0004);
        public static readonly EasyProtocolV2 ClearCount = new EasyProtocolV2(0x0005);

        public EasyProtocolV2(bool isWrite = false)
            : base(2, isWrite)
        {

        }

        public EasyProtocolV2(ushort command)
            : base(2, command)
        {

        }

        public EasyProtocolV2(byte[] bytes)
            : base(2, bytes)
        {

        }

        public EasyProtocolV2(EasyProtocolV2 other)
            : base(other)
        {

        }
    }
}
