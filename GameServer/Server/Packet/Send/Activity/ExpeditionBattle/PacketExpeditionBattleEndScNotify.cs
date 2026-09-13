using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity.ExpeditionBattle;

public class PacketExpeditionBattleEndScNotify : BasePacket
{
    public PacketExpeditionBattleEndScNotify(ExpeditionBattleMapData mapData, bool isNewRecord) : base(CmdIds.ExpeditionBattleEndScNotify)
    {
        SetData(new ExpeditionBattleEndScNotify { MapData = mapData, IsNewRecord = isNewRecord });
    }
}
