namespace Easy.NetV2
{
    public interface IEasyProtocolSender
    {
        IEasyProtocol Send(IEasyProtocol protocol);
        EasyReturnStatus Read(uint address, int size, out byte[] data);
        EasyReturnStatus Write<T>(uint address, T data);
    }
}