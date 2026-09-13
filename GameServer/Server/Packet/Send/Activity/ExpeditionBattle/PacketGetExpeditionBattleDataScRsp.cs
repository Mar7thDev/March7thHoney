using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity.ExpeditionBattle;

public class PacketGetExpeditionBattleDataScRsp : BasePacket
{
    public PacketGetExpeditionBattleDataScRsp(PlayerInstance player) : base(CmdIds.GetExpeditionBattleDataScRsp)
    {
        var proto = new GetExpeditionBattleDataScRsp();
        proto.MapDataList.AddRange(player.ExpeditionBattleManager!.ToProtoMaps());
        SetData(proto);
    }
}
