using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Mission;

public class PacketFinishTalkMissionScRsp : BasePacket
{
    public PacketFinishTalkMissionScRsp(string talkStr) : base(CmdIds.FinishTalkMissionScRsp)
    {
        var proto = new FinishTalkMissionScRsp
        {
            TalkStr = talkStr
        };

        SetData(proto);
    }
}