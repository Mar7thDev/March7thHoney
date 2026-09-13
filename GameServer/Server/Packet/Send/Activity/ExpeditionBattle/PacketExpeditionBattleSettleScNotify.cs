using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity.ExpeditionBattle;

public class PacketExpeditionBattleSettleScNotify : BasePacket
{
    public PacketExpeditionBattleSettleScNotify(ExpeditionBattleSettleScNotify notify) : base(CmdIds.ExpeditionBattleSettleScNotify)
    {
        SetData(notify);
    }
}
