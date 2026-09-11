using March7thHoney.GameServer.Game.Battle;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity.FinalityBattle;

public class PacketStartFinalityBattleLevelScRsp : BasePacket
{
    public PacketStartFinalityBattleLevelScRsp(uint levelId, Retcode retcode, BattleInstance? battle) : base(
        CmdIds.StartFinalityBattleLevelScRsp)
    {
        var proto = new StartFinalityBattleLevelScRsp
        {
            LevelId = levelId,
            Retcode = (uint)retcode
        };
        if (battle != null) proto.BattleInfo = battle.ToProto();
        SetData(proto);
    }
}
