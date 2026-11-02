using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    public class EasyPacket
    {
        private const int MAXSIZE = 65536;


        internal protected byte[] buffer;
        internal protected int size;
        internal protected int pos;

        private int RemainingBytesToTransfer
        {
            get => this.size - this.pos;
        }

        private EasyPacket() { }
        public EasyPacket(int limit)
        {
            this.pos = 0;
            this.size = 0;
            limit = Math.Min(limit, MAXSIZE);

            ResizeBuffer(limit);
        }

        internal protected EasyPacket(byte[] bytes)
        {
            this.pos = 0;
            this.size = bytes.Count();
            ResizeBuffer(this.size);
            Array.Copy(bytes, buffer, this.size);
        }

        internal protected EasyPacket(EasyPacket other)
        {
            this.pos = other.pos;
            this.size = other.size;
            ResizeBuffer(other.size);
            Array.Copy(other.buffer, this.buffer, other.size);
        }

        private bool CanWriteToPacket(int size)
        {
            return this.size + size <= this.buffer.Length;
        }

        private bool CanReadFromPacket(int size)
        {
            return this.pos + size <= this.size;
        }

        internal protected void ResizeBuffer(int size)
        {
            var buffer = new byte[size];
            if (this.buffer != null)
            {
                Array.Copy(this.buffer, buffer, Math.Min(this.buffer.Length, buffer.Length));
            }

            this.buffer = buffer;
        }

        public void SendUint8(byte data)
        {
            var size = Marshal.SizeOf(data);
            Debug.Assert(CanWriteToPacket(size));

            buffer[this.size] = data;
            this.size += size;
        }

        public void SendUInt16(ushort data)
        {
            var size = Marshal.SizeOf(data);
            Debug.Assert(CanWriteToPacket(size));

            buffer[this.size + 0] = (byte)(data & 0xFF);
            buffer[this.size + 1] = (byte)((data >> 8) & 0xFF);
            this.size += size;
        }

        public void SendUInt32(uint data)
        {
            var size = Marshal.SizeOf(data);
            Debug.Assert(CanWriteToPacket(size));

            buffer[this.size + 0] = (byte)(data & 0xFF);
            buffer[this.size + 1] = (byte)((data >> 8) & 0xFF);
            buffer[this.size + 2] = (byte)((data >> 16) & 0xFF);
            buffer[this.size + 3] = (byte)((data >> 24) & 0xFF);
            this.size += size;
        }

        public void SendUint64(ulong data)
        {
            var size = Marshal.SizeOf(data);
            Debug.Assert(CanWriteToPacket(size));

            buffer[this.size + 0] = (byte)(data & 0xFF);
            buffer[this.size + 1] = (byte)((data >> 8) & 0xFF);
            buffer[this.size + 2] = (byte)((data >> 16) & 0xFF);
            buffer[this.size + 3] = (byte)((data >> 24) & 0xFF);
            buffer[this.size + 4] = (byte)((data >> 32) & 0xFF);
            buffer[this.size + 5] = (byte)((data >> 40) & 0xFF);
            buffer[this.size + 6] = (byte)((data >> 48) & 0xFF);
            buffer[this.size + 7] = (byte)((data >> 56) & 0xFF);
            this.size += size;
        }

        public void SendDouble(double data)
        {
            long n = BitConverter.DoubleToInt64Bits(data);
            SendUint64((ulong)n);
        }

        public void SendString(string data)
        {
            var buffer = ASCIIEncoding.ASCII.GetBytes(data);
            SendBuffer(buffer);
        }

        public void SendBuffer(IEnumerable<byte> buffer)
        {
            var size = buffer.Count();
            Debug.Assert(CanWriteToPacket(size));

            int index = 0;
            foreach (var data in buffer)
            {
                this.buffer[this.size + index++] = data;
            }
            this.size += size;
        }

        public sbyte RecvInt8()
        {
            return (sbyte)RecvUInt8();
        }

        public byte RecvUInt8()
        {
            byte n = 0;
            var size = Marshal.SizeOf(n);
            if (CanReadFromPacket(size) == false) return 0;

            n = this.buffer[this.pos++];
            return n;
        }

        public bool RecvBool()
        {
            return RecvUInt8() != 0;
        }

        public short RecvInt16()
        {
            return (short)RecvUInt16();
        }

        public ushort RecvUInt16()
        {
            ushort n = 0;
            var size = Marshal.SizeOf(n);
            if (CanReadFromPacket(size) == false) return 0;

            n = this.buffer[this.pos++];
            n += (ushort)(this.buffer[this.pos++] << 8);
            return n;
        }

        public int RecvInt32()
        {
            return (int)RecvUInt32();
        }

        public uint RecvUInt32()
        {
            uint n = 0;
            var size = Marshal.SizeOf(n);
            if (CanReadFromPacket(size) == false) return 0;

            n = this.buffer[this.pos++];
            n += (uint)(this.buffer[this.pos++] << 8);
            n += (uint)(this.buffer[this.pos++] << 16);
            n += (uint)(this.buffer[this.pos++] << 24);
            return n;
        }

        public long RecvInt64()
        {
            return (long)RecvUInt64();
        }

        public ulong RecvUInt64()
        {
            ulong n = 0;
            var size = Marshal.SizeOf(n);
            if (CanReadFromPacket(size) == false) return 0;

            n = this.buffer[this.pos++];
            n += ((ulong)this.buffer[this.pos++] << 8);
            n += ((ulong)this.buffer[this.pos++] << 16);
            n += ((ulong)this.buffer[this.pos++] << 24);
            n += ((ulong)this.buffer[this.pos++] << 32);
            n += ((ulong)this.buffer[this.pos++] << 40);
            n += ((ulong)this.buffer[this.pos++] << 48);
            n += ((ulong)this.buffer[this.pos++] << 56);
            return n;
        }

        public double RecvDouble()
        {
            ulong n = RecvUInt64();
            return BitConverter.Int64BitsToDouble((long)n);
        }

        public IEnumerable<byte> RecvBuffer(int size)
        {
            try
            {
                Debug.Assert(size > 0 && size <= RemainingBytesToTransfer);
                return buffer.Skip(this.pos).Take(size);
            }
            finally
            {
                this.pos += size;
            }
        }

        public string RecvString(int size)
        {
            var buffer = RecvBuffer(size).ToArray();
            return ASCIIEncoding.ASCII.GetString(buffer).Trim();
        }

        internal protected virtual void PrepareToSend()
        {

        }

        public IEnumerable<byte> GetBytes()
        {
            var originalSize = this.size;
            PrepareToSend();
            var bytes = buffer.Take(size).ToArray();
            this.size = originalSize;
            return bytes;
        }

    }
}
