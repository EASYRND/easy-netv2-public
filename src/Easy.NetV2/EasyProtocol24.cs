using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy.NetV2
{
    public struct EasyProtocol24
    {
        public static readonly EasyProtocol24 Empty;

        private const int PACKETLENGTH = 13;

        private const byte STX = 0x53;
        private const byte ETX = 0x45;
        private const byte OPCODE = 0x20;
        private const byte MAGICCODE = 0x7D;

        #region 속성
        public char Command { get; set; }
        public ushort Address { get; set; }
        public uint Data { get; set; }
        public byte Status { get; set; }

        public byte CheckSum
        {
            get
            {
                return CalcCheckSum(Command, Address, Data, Status);
            }
        }

        public int Length
        {
            get { return PACKETLENGTH; }
        }
        #endregion

        #region 생성자

        public EasyProtocol24(char command, ushort address)
            : this(command, address, 0, 0)
        {

        }

        public EasyProtocol24(char command, ushort address, uint data)
            : this(command, address, data, 0)
        {

        }

        public EasyProtocol24(char command, ushort address, uint data, byte status)
        {
            Command = command;
            Address = address;
            Data = data;
            Status = status;
        }

        #endregion

        public byte[] GetBytes()
        {
            List<byte> list = new List<byte>(20);

            var origin = new byte[] { (byte)Command }
                .Concat(BitConverter.GetBytes(Address))
                .Concat(BitConverter.GetBytes(Data))
                .Concat( new byte[] { Status })
                .Concat( new byte[] { CheckSum });

            list.Add(STX);
            foreach (var x in origin)
            {
                if (x == STX || x == ETX || x == MAGICCODE)
                {
                    list.AddRange(new byte[] { MAGICCODE, (byte)(x ^ OPCODE) });
                }
                else
                {
                    list.Add(x);
                }
            }
            list.Add(ETX);

            return list.ToArray();
        }

        private static byte CalcCheckSum(char command, ushort address, uint data = 0, byte status = 0)
        {
            int sum = command;
            sum += (int)BitConverter.GetBytes(address).Sum(x => x);
            sum += (int)BitConverter.GetBytes(data).Sum(x => x);
            sum += status;

            return (byte)(sum & 0xFF);
        }

        public static string GetStatusString(byte status)
        {
            switch (status)
            {
                case 0x00:
                    return "OK";
                case 0xF1:
                    return "NAK";
                case 0xF2:
                    return "RDY";
                case 0xF3:
                    return "Checksum Error";
                case 0xF4:
                    return "FPGA Data Missmatch";
                case 0xF5:
                    return "Unknown Address";
                case 0xF6:
                    return "Frame Error";
                case 0xF7:
                    return "Flash Missmatch";
                case 0xF8:
                    return "Rangge Error";
                case 0xF9:
                    return "Timeout";
            }

            return $"{status:X2}";
        }

        public static EasyProtocol24 Parse(byte[] buffer, int startIndex = 0)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException();
            }

            int bufferLen = buffer.Length;
            if (startIndex < 0 || startIndex >= bufferLen)
            {
                throw new ArgumentException();
            }

            var buffer2 = buffer.Skip(startIndex).SkipWhile(x => x != STX).Skip(1).TakeWhile(x => x != ETX).ToArray();
            if (buffer2 == null)
            {
                throw new Exception("STX를 찾을 수 없습니다.");
            }

            List<byte> list = new List<byte>(20);
            for (int i = 0; i < buffer2.Length; i++)
            {
                var x = buffer2[i];
                if (x == MAGICCODE)
                {
                    var y = buffer2[++i];
                    list.Add((byte)(y ^ OPCODE));
                }
                else
                {
                    list.Add(x);
                }
            }

            try
            {
                var buffer3 = list.ToArray();
                var command = (char)buffer3[0];
                var address = BitConverter.ToUInt16(buffer3, 1);
                var data = BitConverter.ToUInt32(buffer3, 3);
                var status = buffer3[7];
                var checksum = buffer3[8];

                return new EasyProtocol24(command, address, data, status);
            }
            catch (Exception)
            {
                throw new Exception("데이터 길이가 프로토콜 길이보다 작습니다.");
            }
        }

        public override string ToString()
        {
            return $"[{Command}][{Address}({Address:X4})][{Data}({Data:X8})][{GetStatusString(Status)}][{CheckSum:X2}]";
        }
    }
}
