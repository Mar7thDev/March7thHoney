using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity.ExpeditionBattle;

public class PacketExpeditionBattleStartScRsp : BasePacket
{
    public PacketExpeditionBattleStartScRsp(Retcode retcode) : base(CmdIds.ExpeditionBattleStartScRsp)
    {
        SetData(new ExpeditionBattleStartScRsp { Retcode = (uint)retcode });
    }
}
