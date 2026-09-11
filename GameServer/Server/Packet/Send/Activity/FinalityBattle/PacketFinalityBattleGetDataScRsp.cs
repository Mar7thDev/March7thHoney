using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity.FinalityBattle;

public class PacketFinalityBattleGetDataScRsp : BasePacket
{
    public PacketFinalityBattleGetDataScRsp(PlayerInstance player) : base(CmdIds.FinalityBattleGetDataScRsp)
    {
        var proto = new FinalityBattleGetDataScRsp();
        proto.LevelRecordList.AddRange(player.FinalityBattleManager!.ToProtoRecords());
        SetData(proto);
    }
}
