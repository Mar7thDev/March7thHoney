using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity.ExpeditionBattle;

public class PacketExpeditionBattleSetLineupScRsp : BasePacket
{
    public PacketExpeditionBattleSetLineupScRsp(Retcode retcode, ExpeditionBattleSetLineupCsReq req)
        : base(CmdIds.ExpeditionBattleSetLineupScRsp)
    {
        var proto = new ExpeditionBattleSetLineupScRsp
        {
            Retcode = (uint)retcode,
            MapId = req.MapId
        };
        if (retcode == Retcode.RetSucc) proto.LineupList.AddRange(req.LineupList);
        SetData(proto);
    }
}
