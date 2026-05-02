using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.TalkEvent;

public class PacketSelectInclinationTextScRsp : BasePacket
{
    public PacketSelectInclinationTextScRsp(uint id) : base(CmdIds.SelectInclinationTextScRsp)
    {
        var proto = new SelectInclinationTextScRsp
        {
            TalkSentenceId = id
        };

        SetData(proto);
    }
}