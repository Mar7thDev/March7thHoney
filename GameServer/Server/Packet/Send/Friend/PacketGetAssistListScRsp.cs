using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketGetAssistListScRsp : BasePacket
{
    public PacketGetAssistListScRsp() : base(CmdIds.GetAssistListScRsp)
    {
        var proto = new GetAssistListScRsp();

        SetData(proto);
    }
}