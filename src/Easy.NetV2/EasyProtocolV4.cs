using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    public class EasyProtocolV4 : EasyProtocol, IEasyProtocol, IEasyProtocolCommand
    {
        private const int DEFAULT_FRAME_CAPACITY = 1024;
        private const int RECEIVE_MARGIN = 1024;

        public ushort Command { get; private set; }
        public uint Address { get; private set; }
        public uint DataLength { get; private set; }

        public byte FRAME_VERSION => 4;

        public int TargetLength
        {
            get
            {
                return (int)DataLength + RECEIVE_MARGIN;
            }
        }

        public ushort Status
        {
            get
            {
                if (Command != 3)
                {
                    return 0;
                }
                if (this.buffer.Length < 19)
                {
                    return 0;
                }
                return BitConverter.ToUInt16(this.buffer, 17);
            }
        }

        public EasyProtocolV4(bool isWrite = false)
            : base(4, DEFAULT_FRAME_CAPACITY)
        {
            Command = (ushort)(isWrite ? 1 : 0);
            SendUInt16(Command);
        }

        public EasyProtocolV4(ushort command)
            : base(4, DEFAULT_FRAME_CAPACITY)
        {
            Command = command;
            SendUInt16(command);
        }

        private EasyProtocolV4(byte[] bytes)
            : base(bytes, 4)
        {
            Command = RecvUInt16();
            Address = RecvUInt32();
            DataLength = RecvUInt32();
        }

        public EasyProtocolV4(EasyProtocolV4 other)
            : base(other)
        {
            Command = other.Command;
            Address = other.Address;
            DataLength = other.DataLength;
        }

        public void SetData(uint address, int size, IEnumerable<byte> buffer)
        {
            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            byte[] payload = null;
            if (buffer != null)
            {
                payload = buffer as byte[] ?? buffer.ToArray();
                if (payload.Length != size)
                {
                    throw new ArgumentException($"The specified size '{size}' does not match buffer length '{payload.Length}'.", nameof(size));
                }

                var required = DEFAULT_FRAME_CAPACITY + size;
                if (this.buffer.Length < required)
                {
                    ResizeBuffer(required);
                }
            }

            Address = address;
            DataLength = (uint)size;
            SendUInt32(address);
            SendUInt32((uint)size);
            if (payload != null)
            {
                SendBuffer(payload);
            }
        }

        protected internal override void PrepareToSend()
        {
            const int SOFSIZE = 2;
            const int VERSIONSIZE = 1;
            const int SIZESIZE = 4;
            var size = this.size - SOFSIZE - VERSIONSIZE - SIZESIZE;
            var temp = BitConverter.GetBytes(size);

            this.buffer[3] = temp[0];
            this.buffer[4] = temp[1];
            this.buffer[5] = temp[2];
            this.buffer[6] = temp[3];

            this.pos = 0;
        }

        public IEasyProtocol CreateNewObject(byte[] bytes)
        {
            return new EasyProtocolV4(bytes);
        }
    }
}
