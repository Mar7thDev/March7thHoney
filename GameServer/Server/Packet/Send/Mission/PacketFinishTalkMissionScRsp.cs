using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Mission;

public class PacketFinishTalkMissionScRsp : BasePacket
{
    public PacketFinishTalkMissionScRsp(FinishTalkMissionCsReq req) : base(CmdIds.FinishTalkMissionScRsp)
    {
        var proto = new FinishTalkMissionScRsp
        {
            TalkStr = req.TalkStr,
            SubMissionId = req.SubMissionId
        };
        proto.CustomValueList.AddRange(req.CustomValueList);

        SetData(proto);
    }
}
