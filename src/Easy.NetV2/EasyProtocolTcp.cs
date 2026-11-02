using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    public static class SLLBAddresses
    {
        public static readonly EasyDeviceItemDouble EpcDeviceMode = new EasyDeviceItemDouble("EpcDeviceMode", 0x00000001, 3, false);
    }
    public class EasyDeviceItem
    {
        internal string Name { get; }
        internal uint Address { get; }
        internal int Size { get; }

        private EasyDeviceItem() { }
        protected EasyDeviceItem(string name, uint address, int size)
        {
            Name = name;
            Address = address;
            Size = size;
        }
    }

    public class EasyDeviceItemDouble : EasyDeviceItem
    {
        public int DecimalPlaces { get; }
        public bool IsSigned { get; }
        public EasyDeviceItemDouble(string name, uint address, int decimalPlaces, bool isSigned)
            : base(name, address, 4)
        {
            DecimalPlaces = decimalPlaces;
            IsSigned = isSigned;
        }

        public uint ConvertTo(double value)
        {
            var scale = Math.Pow(10, DecimalPlaces);
            var scaled = Math.Round(value * scale, MidpointRounding.AwayFromZero);

            if (IsSigned)
            {
                if (scaled < int.MinValue || scaled > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"The value '{value}' is out of signed 32-bit range after scaling.");
                }
                return unchecked((uint)(int)scaled);
            }

            if (scaled < uint.MinValue || scaled > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"The value '{value}' is out of unsigned 32-bit range after scaling.");
            }
            return (uint)scaled;
        }

        public double ConvertFrom(uint value)
        {
            var scale = Math.Pow(10, DecimalPlaces);
            if (IsSigned)
            {
                return (int)value / scale;
            }
            return value / scale;
        }
    }

    public class EasyDeviceAddressString : EasyDeviceItem
    {
        public EasyDeviceAddressString(string name, uint address, int size)
            : base(name, address, size)
        {

        }
    }

    public class EasyReturnValue<T>
    {
        internal IEasyProtocolSenderGroup Sender { get; }
        internal EasyDeviceItem Item { get; }
        public virtual T Value { get; }

        private EasyReturnValue() { }
        internal EasyReturnValue(IEasyProtocolSenderGroup sender, EasyDeviceItem item)
        {
            Sender = sender;
            Item = item;
        }

        public virtual EasyReturnStatus Set(T value)
        {
            throw new NotSupportedException($"Set is not supported for '{typeof(T).Name}'.");
        }
    }

    public class EasyReturnDouble : EasyReturnValue<double>
    {
        internal EasyReturnDouble(IEasyProtocolSenderGroup sender, EasyDeviceItem item)
            : base(sender, item) { }

        public override double Value
        {
            get
            {
                if (Sender == null)
                {
                    throw new NullReferenceException(nameof(Sender));
                }

                var item = Item as EasyDeviceItemDouble;
                if (item == null)
                {
                    throw new InvalidOperationException("Invalid device item type.");
                }

                byte[] data;
                var status = Sender.Read(item.Address, item.Size, out data);
                if (status != EasyReturnStatus.Success)
                {
                    throw new InvalidOperationException($"Read failed: {status}");
                }
                if (data == null || data.Length != item.Size)
                {
                    throw new InvalidOperationException("Invalid response size.");
                }

                return item.ConvertFrom(BitConverter.ToUInt32(data, 0));
            }
        }

        public override EasyReturnStatus Set(double value)
        {
            if (Sender == null)
            {
                throw new NullReferenceException(nameof(Sender));
            }

            var item = Item as EasyDeviceItemDouble;
            if (item == null)
            {
                throw new InvalidOperationException("Invalid device item type.");
            }

            return Sender.Write(item.Address, item.ConvertTo(value));
        }
    }

    public class EasyProtocolTcp : EasyTcp, IEasyProtocolSenderGroup
    {
        private readonly object sendSync = new object();
        private byte[] receiveBuffer = new byte[4096];
        public EasyReturnStatus LastSendStatus { get; private set; } = EasyReturnStatus.Success;

        private byte[] EnsureReceiveBuffer(int requiredSize)
        {
            if (requiredSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredSize));
            }

            if (receiveBuffer == null || receiveBuffer.Length < requiredSize)
            {
                receiveBuffer = new byte[requiredSize];
            }
            return receiveBuffer;
        }

        private static bool IsChecksumValid(byte[] responseBuffer)
        {
            if (responseBuffer == null || responseBuffer.Length < 9)
            {
                return false;
            }

            var payloadSize = BitConverter.ToInt32(responseBuffer, 3);
            var totalSize = payloadSize + 9;
            if (payloadSize < 0 || responseBuffer.Length < totalSize)
            {
                return false;
            }

            var checksumIndex = totalSize - 2;
            var checksum = BitConverter.ToUInt16(responseBuffer, checksumIndex);
            int sum = 0;
            for (int i = 0; i < checksumIndex; i++)
            {
                sum += responseBuffer[i];
            }

            return (ushort)sum == checksum;
        }

        private static EasyReturnStatus MapLocalException(Exception ex)
        {
            if (ex is TimeoutException)
            {
                return EasyReturnStatus.ResponseTimeout;
            }
            if (ex is ArgumentException)
            {
                return EasyReturnStatus.InvalidArgument;
            }
            if (ex is NotSupportedException)
            {
                return EasyReturnStatus.UnsupportedDataType;
            }
            if (ex is InvalidDataException)
            {
                return EasyReturnStatus.InvalidProtocolFrame;
            }
            if (ex is IOException || ex is SocketException || ex is ObjectDisposedException)
            {
                return EasyReturnStatus.ConnectionClosed;
            }

            return EasyReturnStatus.InternalError;
        }

        private bool TrySend(IEasyProtocol protocol, out IEasyProtocol response, out EasyReturnStatus status)
        {
            response = null;
            status = EasyReturnStatus.Success;

            if (protocol == null)
            {
                status = EasyReturnStatus.InvalidArgument;
                return false;
            }

            try
            {
                lock (sendSync)
                {
                    var buffer = protocol.GetBytes().ToArray();
                    var targetLength = protocol.TargetLength;
                    var readBuffer = EnsureReceiveBuffer(targetLength);
                    var recevedCount = WriteAndRead(buffer, 0, buffer.Length, readBuffer, 0, readBuffer.Length);
                    if (recevedCount <= 0)
                    {
                        status = EasyReturnStatus.ResponseEmpty;
                        return false;
                    }

                    var responseBytes = readBuffer.Take(recevedCount).ToArray();
                    response = protocol.CreateNewObject(responseBytes);
                    var easyProtocol = response as EasyProtocol;
                    if (easyProtocol != null && easyProtocol.IsValidated == false)
                    {
                        status = EasyReturnStatus.ResponseValidationFailed;
                        response = null;
                        return false;
                    }
                    if (response is EasyProtocolV0 && IsChecksumValid(responseBytes) == false)
                    {
                        status = EasyReturnStatus.ChecksumMismatch;
                        response = null;
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                status = MapLocalException(ex);
                response = null;
                return false;
            }
        }

        public EasyReturnDouble this[EasyDeviceItemDouble address]
        {
            get
            {
                return new EasyReturnDouble(this, address);
            }
        }

        public byte FrameVersion { get; set; }

        public EasyProtocolTcp(string ip)
            : base(ip, 6313)
        {
            IsEasyProtocol = true;
            Timeout = 10000;
#if EORIC
            FrameVersion = 2;
#else
            FrameVersion = 0;
#endif
        }

        public EasyReturnStatus Command(ushort command)
        {
            return Command(new EasyProtocolGroup(FrameVersion, command));
        }

        protected EasyReturnStatus Command(EasyProtocolGroup protocol)
        {
            IEasyProtocol response;
            EasyReturnStatus status;
            if (TrySend(protocol, out response, out status) == false)
            {
                LastSendStatus = status;
                return status;
            }
            LastSendStatus = EasyReturnStatus.Success;

            var commandResponse = response as IEasyProtocolCommand;
            if (commandResponse is null)
            {
                return EasyReturnStatus.ResponseTypeMismatch;
            }

            return commandResponse.Command == 2 ? EasyReturnStatus.Success : EasyReturnStatus.CommandNotSupported;
        }

        public EasyProtocolGroup Send(EasyProtocolGroup protocol)
        {
            IEasyProtocol response;
            EasyReturnStatus status;
            if (TrySend(protocol, out response, out status))
            {
                LastSendStatus = EasyReturnStatus.Success;
                return response as EasyProtocolGroup;
            }
            LastSendStatus = status;
            return null;
        }

        public IEasyProtocol Send(IEasyProtocol protocol)
        {
            if (protocol is EasyProtocolGroup group)
            {
                return Send(group);
            }

            IEasyProtocol response;
            EasyReturnStatus status;
            if (TrySend(protocol, out response, out status))
            {
                LastSendStatus = EasyReturnStatus.Success;
                return response;
            }

            LastSendStatus = status;
            return null;
        }

        public EasyReturnStatus Read(uint address, int size, out byte[] data)
        {
            data = null;
            if (size < 0)
            {
                return EasyReturnStatus.InvalidArgument;
            }

            EasyProtocolGroup protocol = FrameVersion == 0
                ? new EasyProtocolV0()
                : new EasyProtocolGroup(FrameVersion);
            protocol.SetGroup(address, size, null);

            IEasyProtocol response;
            EasyReturnStatus sendStatus;
            if (TrySend(protocol, out response, out sendStatus) == false)
            {
                return sendStatus;
            }

            var returnValue = response as EasyProtocolGroup;
            if (returnValue == null)
            {
                return EasyReturnStatus.ResponseTypeMismatch;
            }
            if (returnValue.Status != 0)
            {
                return (EasyReturnStatus)returnValue.Status;
            }

            data = returnValue.RecvBuffer(size).ToArray();
            if (data.Length != size)
            {
                data = null;
                return EasyReturnStatus.ResponseSizeMismatch;
            }

            return EasyReturnStatus.Success;
        }

        public EasyReturnStatus Write<T>(uint address, T data)
        {
            if (data == null)
            {
                return EasyReturnStatus.InvalidArgument;
            }

            EasyProtocolGroup protocol = FrameVersion == 0
                ? new EasyProtocolV0(true)
                : new EasyProtocolGroup(FrameVersion, true);
            try
            {
                if (data is byte byteData)
                {
                    protocol.SetGroup(address, 1, new byte[] { byteData });
                }
                else if (data is ushort ushortData)
                {
                    protocol.SetGroup(address, 2, BitConverter.GetBytes(ushortData));
                }
                else if (data is short shortData)
                {
                    protocol.SetGroup(address, 2, BitConverter.GetBytes(shortData));
                }
                else if (data is uint uintData)
                {
                    protocol.SetGroup(address, 4, BitConverter.GetBytes(uintData));
                }
                else if (data is int intData)
                {
                    protocol.SetGroup(address, 4, BitConverter.GetBytes(intData));
                }
                else if (data is double doubleData)
                {
                    protocol.SetGroup(address, 8, BitConverter.GetBytes(doubleData));
                }
                else if (data is string stringData)
                {
                    var dataBuffer = Encoding.ASCII.GetBytes(stringData);
                    protocol.SetGroup(address, dataBuffer.Length, dataBuffer);
                }
                else if (data.GetType().IsValueType)
                {
                    int size = System.Runtime.InteropServices.Marshal.SizeOf(data.GetType());
                    byte[] buffer = new byte[size];
                    IntPtr ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
                    try
                    {
                        System.Runtime.InteropServices.Marshal.StructureToPtr(data, ptr, false);
                        System.Runtime.InteropServices.Marshal.Copy(ptr, buffer, 0, size);
                    }
                    finally
                    {
                        System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
                    }
                    protocol.SetGroup(address, buffer.Length, buffer);
                }
                else
                {
                    return EasyReturnStatus.UnsupportedDataType;
                }
            }
            catch (Exception ex)
            {
                return MapLocalException(ex);
            }

            IEasyProtocol response;
            EasyReturnStatus sendStatus;
            if (TrySend(protocol, out response, out sendStatus) == false)
            {
                return sendStatus;
            }

            var returnValue = response as EasyProtocolGroup;
            if (returnValue == null)
            {
                return EasyReturnStatus.ResponseTypeMismatch;
            }
            if (returnValue.Status != 0)
            {
                return (EasyReturnStatus)returnValue.Status;
            }

            return EasyReturnStatus.Success;
        }
    }
}
