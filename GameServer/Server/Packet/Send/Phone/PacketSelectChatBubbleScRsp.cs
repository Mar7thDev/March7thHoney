using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Phone;

public class PacketSelectChatBubbleScRsp : BasePacket
{
    public PacketSelectChatBubbleScRsp(uint bubbleId) : base(CmdIds.SelectChatBubbleScRsp)
    {
        var proto = new SelectChatBubbleScRsp
        {
            CurChatBubble = bubbleId
        };

        SetData(proto);
    }
}