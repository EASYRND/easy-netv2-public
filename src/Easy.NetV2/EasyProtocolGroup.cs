using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easy.NetV2;

namespace Easy.NetV2
{
    public class EasyProtocolGroup : EasyProtocol, IEasyProtocol, IEasyProtocolCommand
    {
        private int groupCount;

        public ushort Command { get; private set; }
        public uint DataSize { get; private set; }
        public ushort Status
        {
            get
            {
                if (Command != 3)
                {
                    return 0;
                }
                if (this.buffer.Length < 15)
                {
                    return 0;
                }
                return BitConverter.ToUInt16(this.buffer, 13);
            }
        }

        public EasyProtocolGroup(byte frameVersion, bool isWrite = false)
            : base(frameVersion, 65536)
        {
            SendUInt16((ushort)(isWrite ? 1 : 0));
            SendUInt16(0);
        }

        public EasyProtocolGroup(byte frameVersion, ushort command)
            : base(frameVersion, 1024)
        {
            SendUInt16(command);
            SendUInt16(0);
        }

        public EasyProtocolGroup(byte frameVersion, byte[] bytes)
            : base(bytes, frameVersion)
        {
            Command = RecvUInt16();
            DataSize = RecvUInt32();
        }

        public EasyProtocolGroup(EasyProtocolGroup other)
            : base(other)
        {
            groupCount = other.groupCount;
            Command = other.Command;
            DataSize = other.DataSize;
        }

        public void SetGroup(uint address, int size, IEnumerable<byte> buffer)
        {
            SendUInt32(address);
            SendUInt32((uint)size);
            if (buffer != null)
            {
                SendBuffer(buffer);
            }
            groupCount++;
        }

        protected internal override void PrepareToSend()
        {
            var temp = BitConverter.GetBytes((ushort)groupCount);
            this.buffer[9] = temp[0];
            this.buffer[10] = temp[1];

            base.PrepareToSend();
        }

        public int TargetLength => 65536;

        public IEasyProtocol CreateNewObject(byte[] bytes)
        {
            return new EasyProtocolGroup(FrameVersion, bytes);
        }
    }
}
