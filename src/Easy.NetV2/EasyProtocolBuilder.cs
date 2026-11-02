using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy.NetV2
{
    public class EasyProtocolBuilder
    {
        private enum ProtocolMode
        {
            V0,
            V4
        }

        private ProtocolMode mode;
        private EasyProtocolV0 protocolV0;
        private EasyProtocolV4 protocolV4;

        public EasyProtocolBuilder()
        {
            mode = ProtocolMode.V0;
            protocolV0 = new EasyProtocolV0();
        }

        private void EnsureV0()
        {
            if (mode != ProtocolMode.V0 || protocolV0 == null)
            {
                throw new InvalidOperationException("Current builder mode is not V0.");
            }
        }

        private void EnsureV4()
        {
            if (mode != ProtocolMode.V4 || protocolV4 == null)
            {
                throw new InvalidOperationException("Current builder mode is not V4.");
            }
        }

        public EasyProtocolBuilder Read()
        {
            mode = ProtocolMode.V0;
            protocolV0 = new EasyProtocolV0(false);
            protocolV4 = null;
            return this;
        }

        public EasyProtocolBuilder Write()
        {
            mode = ProtocolMode.V0;
            protocolV0 = new EasyProtocolV0(true);
            protocolV4 = null;
            return this;
        }

        public EasyProtocolBuilder Command(ushort command)
        {
            mode = ProtocolMode.V0;
            protocolV0 = new EasyProtocolV0(command);
            protocolV4 = null;
            return this;
        }

        public EasyProtocolBuilder V4Read()
        {
            mode = ProtocolMode.V4;
            protocolV4 = new EasyProtocolV4(false);
            protocolV0 = null;
            return this;
        }

        public EasyProtocolBuilder V4Write()
        {
            mode = ProtocolMode.V4;
            protocolV4 = new EasyProtocolV4(true);
            protocolV0 = null;
            return this;
        }

        public EasyProtocolBuilder V4Command(ushort command)
        {
            mode = ProtocolMode.V4;
            protocolV4 = new EasyProtocolV4(command);
            protocolV0 = null;
            return this;
        }

        public EasyProtocolBuilder Group()
        {
            EnsureV0();
            return this;
        }

        public EasyProtocolBuilder Group(uint address, int size, IEnumerable<byte> buffer = null)
        {
            EnsureV0();
            protocolV0.SetGroup(address, size, buffer);
            return this;
        }

        public EasyProtocolBuilder Data(uint address, int size, IEnumerable<byte> buffer = null)
        {
            EnsureV4();
            protocolV4.SetData(address, size, buffer);
            return this;
        }

        public EasyProtocol Build()
        {
            if (mode == ProtocolMode.V0)
            {
                EnsureV0();
                return new EasyProtocolV0(protocolV0);
            }

            EnsureV4();
            return new EasyProtocolV4(protocolV4);
        }
    }
}
