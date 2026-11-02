using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    public class EasyTcp : IDisposable
    {
        private TcpClient tcp;
        private string ip;
        private int port;
        private int timeoutCount;

        private readonly object lockObject = new object();
        public bool IsOpened => tcp?.Connected ?? false;

        public int Timeout
        {
            get => tcp?.ReceiveTimeout ?? 0;
            set
            {
                if (tcp != null && tcp.ReceiveTimeout != value)
                {
                    tcp.ReceiveTimeout = value;
                    Stream.ReadTimeout = value;
                }
            }
        }

        public bool IsEasyProtocol { get; protected set; }

        public Socket Socket => tcp?.Client;
        public NetworkStream Stream => tcp?.GetStream();

        public event EventHandler<EasyTcpConnectionChangedEventArgs> ConnectionChanged;
        public event EventHandler<EventArgs> Error;

        protected EasyTcp(string ip, int port = 6313)
        {
            this.ip = ip;
            this.port = port;
            timeoutCount = 5;
        }

        public bool Open()
        {
            Close();

            tcp = new TcpClient();

            var result = tcp.BeginConnect(ip, port, null, null);

            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

            if (success == false)
            {
                throw new TimeoutException("Failed to connect.");
            }
            else
            {
                timeoutCount = 5;
                ConnectionChanged?.Invoke(this, new EasyTcpConnectionChangedEventArgs(EasyTcpConnectionType.Connected));
            }

            tcp.EndConnect(result);

            return IsOpened;
        }

        public void Close()
        {
            if (tcp != null)
            {
                ConnectionChanged?.Invoke(this, new EasyTcpConnectionChangedEventArgs(EasyTcpConnectionType.Disconnected));
                if (tcp.Connected)
                {
                    tcp.Client.Disconnect(false);
                    tcp.Client.Close();
                }
                tcp.Dispose();
                tcp = null;
            }
        }

        public bool IsConnected()
        {
            return tcp.Connected;
        }

        protected void Write(byte[] buffer, int offset, int size)
        {
            if (tcp == null) return;
            NetworkStream stream = tcp.GetStream();
            if (stream == null) return;
            stream.Write(buffer, offset, size);
            Debug.WriteLine($"=> {BitConverter.ToString(buffer, offset, size)}");
        }

        protected int Read(byte[] buffer, int offset, int size, byte expectedFrameVersion)
        {
            if (tcp == null) return 0;
            NetworkStream stream = tcp.GetStream();
            if (stream == null) return 0;
            if (IsEasyProtocol)
            {
                return ReadEasyProtocolFrame(stream, buffer, offset, size, expectedFrameVersion);
            }

            int receivedCount = 0;
            timeoutCount = 5;

            while (true)
            {
                int remainingSize = size - receivedCount;
                if (remainingSize <= 0)
                {
                    break;
                }

                int received = ReadFromStream(stream, buffer, offset + receivedCount, remainingSize);
                if (received == 0)
                {
                    throw new IOException("The remote host closed the connection.");
                }

                receivedCount += received;
                if (stream.DataAvailable == false)
                {
                    break;
                }
            }

            Debug.WriteLine($"ReceivedCount={receivedCount}");
            Debug.WriteLine($"<= {BitConverter.ToString(buffer, offset, Math.Min(receivedCount, 64))}");
            return receivedCount;
        }

        private int ReadEasyProtocolFrame(
            NetworkStream stream,
            byte[] buffer,
            int offset,
            int size,
            byte expectedFrameVersion)
        {
            const byte SofLow = 0x10;
            const byte SofHigh = 0x02;
            const int HeaderSize = 7;
            const int FrameOverhead = 9;
            const int MaxFrameSize = 33_554_441;

            var header = new byte[HeaderSize];
            timeoutCount = 5;

            while (true)
            {
                ReadToStartOfFrame(stream, SofLow, SofHigh);
                header[0] = SofLow;
                header[1] = SofHigh;
                ReadExactly(stream, header, 2, HeaderSize - 2);

                byte frameVersion = header[2];
                int frameSize = BitConverter.ToInt32(header, 3) + FrameOverhead;

                if (frameSize < FrameOverhead || frameSize > MaxFrameSize)
                {
                    Debug.WriteLine($"Ignoring invalid protocol frame size: {frameSize}");
                    continue;
                }

                int remainingFrameSize = frameSize - HeaderSize;
                bool isKnownVersion = frameVersion == 0 || frameVersion == 4;
                bool isExpectedFrame = frameVersion == expectedFrameVersion && frameSize <= size;

                if (isExpectedFrame == false)
                {
                    Debug.WriteLine(
                        $"Discarding protocol frame. Version={frameVersion}, Size={frameSize}, ExpectedVersion={expectedFrameVersion}");

                    if (isKnownVersion)
                    {
                        DrainExactly(stream, remainingFrameSize);
                    }
                    continue;
                }

                Array.Copy(header, 0, buffer, offset, HeaderSize);
                ReadExactly(stream, buffer, offset + HeaderSize, remainingFrameSize);

                Debug.WriteLine($"TargetSize={frameSize}");
                Debug.WriteLine($"ReceivedCount={frameSize}");
                Debug.WriteLine($"<= {BitConverter.ToString(buffer, offset, Math.Min(frameSize, 64))}");

                return frameSize;
            }
        }

        private void ReadToStartOfFrame(NetworkStream stream, byte sofLow, byte sofHigh)
        {
            var value = new byte[1];
            bool foundFirstByte = false;

            while (true)
            {
                ReadExactly(stream, value, 0, 1);

                if (foundFirstByte && value[0] == sofHigh)
                {
                    return;
                }

                foundFirstByte = value[0] == sofLow;
            }
        }

        private void ReadExactly(NetworkStream stream, byte[] buffer, int offset, int size)
        {
            int receivedCount = 0;

            while (receivedCount < size)
            {
                int received = ReadFromStream(
                    stream,
                    buffer,
                    offset + receivedCount,
                    size - receivedCount);

                if (received == 0)
                {
                    throw new IOException("The remote host closed the connection.");
                }

                receivedCount += received;
            }
        }

        private void DrainExactly(NetworkStream stream, int size)
        {
            var discardBuffer = new byte[Math.Min(size, 8192)];
            int drainedCount = 0;

            while (drainedCount < size)
            {
                int readSize = Math.Min(discardBuffer.Length, size - drainedCount);
                int received = ReadFromStream(stream, discardBuffer, 0, readSize);

                if (received == 0)
                {
                    throw new IOException("The remote host closed the connection.");
                }

                drainedCount += received;
            }
        }

        private int ReadFromStream(NetworkStream stream, byte[] buffer, int offset, int size)
        {
            try
            {
                return stream.Read(buffer, offset, size);
            }
            catch (IOException exception) when (IsReadTimeout(exception))
            {
                if (--timeoutCount <= 0)
                {
                    Error?.Invoke(this, EventArgs.Empty);
                }

                throw new TimeoutException("The network read timed out.", exception);
            }
        }

        private static bool IsReadTimeout(IOException exception)
        {
            var socketException = exception.InnerException as SocketException;
            return socketException?.SocketErrorCode == SocketError.TimedOut;
        }

        protected int WriteAndRead(byte[] writebuffer, int writeOffset, int writeSize, byte[] readBuffer, int readOffset, int readSize)
        {
            var readCount = 0;
            lock (lockObject)
            {
                Write(writebuffer, writeOffset, writeSize);
                byte expectedFrameVersion = writeSize >= 3
                    ? writebuffer[writeOffset + 2]
                    : (byte)0;
                readCount = Read(readBuffer, readOffset, readSize, expectedFrameVersion);
            }
            return readCount;
        }


        #region IDisposable Support
        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (disposed == false)
            {
                if (disposing)
                {
                    Close();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
