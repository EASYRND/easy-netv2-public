using System.Collections.Generic;

namespace Easy.NetV2
{
    public interface IEasyProtocol
    {
        IEnumerable<byte> GetBytes();
        IEasyProtocol CreateNewObject(byte[] bytes);
        int TargetLength { get; }
    }
}
