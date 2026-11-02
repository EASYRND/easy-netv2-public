namespace Easy.NetV2
{
    public interface IEasyProtocolSenderGroup : IEasyProtocolSender
    {
        EasyProtocolGroup Send(EasyProtocolGroup protocol);
    }
}