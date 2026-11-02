using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    public enum EasyReturnStatus : ushort
    {
        Success = 0,
        //DEVICE(REMOTE)
        NotOK,
        NotReady,
        FrameError,
        CommandError,
        AddressError,
        SizeError,
        CheckSumError,
        FPGAError,
        FlashError,
        RangeError,
        DeviceOperationTimeout,
        CrcError,
        //PC(LOCAL)
        NoWritable = 0x100,
        ResponseTimeout,
        CommandNotSupported,
        InvalidArgument,
        UnsupportedDataType,
        ResponseEmpty,
        ResponseValidationFailed,
        ChecksumMismatch,
        InvalidProtocolFrame,
        ResponseTypeMismatch,
        ResponseSizeMismatch,
        UnexpectedResponseCommand,
        ConnectionClosed,
        InternalError
    }

    public class EasyProtocol : EasyPacket
    {
        private const ushort SOF = 0x0210;

        #region 속성
        public bool IsValidated { get; }
        public byte FrameVersion { get; protected set; }
        public int FrameSize { get; protected set; }

        #endregion

        #region 생성자

        public EasyProtocol(byte frameVersion, int frameSize)
            : base(frameSize)
        {
            FrameVersion = frameVersion;
            SendUInt16(SOF);
            SendUint8(frameVersion);
            SendUInt32(0);
        }

        internal EasyProtocol(byte[] bytes, byte frameVersion = 0)
            : base(bytes)
        {
            var sof = RecvUInt16();
            FrameVersion = RecvUInt8();
            IsValidated = (sof == SOF && FrameVersion == frameVersion);
            FrameSize = (int)RecvUInt32();
        }

        internal EasyProtocol(EasyProtocol other)
            : base(other)
        {
            IsValidated = other.IsValidated;
            FrameVersion = other.FrameVersion;
            FrameSize = other.FrameSize;
        }

        #endregion


        public virtual void PrepareToRead()
        {

        }

        internal protected override void PrepareToSend()
        {
            // SOF:2, Version:1, Size=4, Checksum:2;
            const int SOFSIZE = 2;
            const int VERSIONSIZE = 1;
            const int SIZESIZE = 4;
            var size = this.size - SOFSIZE - VERSIONSIZE - SIZESIZE;
            var temp = BitConverter.GetBytes(size);
            this.buffer[3] = temp[0];
            this.buffer[4] = temp[1];
            this.buffer[5] = temp[2];
            this.buffer[6] = temp[3];

            ushort checksum = (ushort)this.buffer.Take(this.size).Select(x => (int)x).Sum();
            SendUInt16(checksum);

            this.pos = 0;
        }

        public EasyReturnStatus TransferOut(Stream stream)
        {
            if (stream  == null)
            {
                return EasyReturnStatus.InvalidArgument;
            }
            Debug.Assert(stream.CanWrite);
            if (stream.CanWrite == false)
            {
                return EasyReturnStatus.NoWritable;
            }

            PrepareToSend();

            var buffer = this.GetBytes().ToArray();
            stream.Write(buffer, 0, buffer.Length);
            Debug.WriteLine(BitConverter.ToString(buffer));

            //checksum 없애기
            this.size -= 2;

            return EasyReturnStatus.Success;
        }
    }
}
