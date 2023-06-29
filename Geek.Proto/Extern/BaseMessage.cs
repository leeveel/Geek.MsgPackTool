using MessagePack;

//namespace Core.Net;

[MessagePackObject(true)]

public class BaseMessage
{
    /// <summary>
    /// 消息唯一id
    /// </summary>
    public int UniId { get; set; }

    [IgnoreMember] public virtual int MsgId { get; }
}