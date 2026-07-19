using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ExpeditionBattle;

public class PacketGetExpeditionBattleDataScRsp : BasePacket
{
    public PacketGetExpeditionBattleDataScRsp(GetExpeditionBattleDataScRsp response)
        : base(CmdIds.GetExpeditionBattleDataScRsp)
    {
        SetData(response);
    }
}

public class PacketExpeditionBattleSetLineupScRsp : BasePacket
{
    public PacketExpeditionBattleSetLineupScRsp(ExpeditionBattleSetLineupCsReq request, Retcode retcode)
        : base(CmdIds.ExpeditionBattleSetLineupScRsp)
    {
        var response = new ExpeditionBattleSetLineupScRsp
        {
            MapId = request.MapId,
            Retcode = (uint)retcode
        };
        response.LineupList.Add(request.LineupList);
        SetData(response);
    }
}

public class PacketExpeditionBattleStartScRsp : BasePacket
{
    public PacketExpeditionBattleStartScRsp(Retcode retcode = Retcode.RetSucc)
        : base(CmdIds.ExpeditionBattleStartScRsp)
    {
        SetData(new ExpeditionBattleStartScRsp { Retcode = (uint)retcode });
    }
}

public class PacketExpeditionBattleSettleScNotify : BasePacket
{
    public PacketExpeditionBattleSettleScNotify(ExpeditionBattleSettleScNotify notify)
        : base(CmdIds.ExpeditionBattleSettleScNotify)
    {
        SetData(notify);
    }
}

public class PacketExpeditionBattleEndScNotify : BasePacket
{
    public PacketExpeditionBattleEndScNotify(ExpeditionBattleEndScNotify notify)
        : base(CmdIds.ExpeditionBattleEndScNotify)
    {
        SetData(notify);
    }
}
