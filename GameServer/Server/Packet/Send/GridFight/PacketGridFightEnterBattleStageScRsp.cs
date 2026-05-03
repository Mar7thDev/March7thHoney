using March7thHoney.GameServer.Game.Battle;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightEnterBattleStageScRsp : BasePacket
{
    public PacketGridFightEnterBattleStageScRsp(BattleInstance? inst) : base(CmdIds.GridFightEnterBattleStageScRsp)
    {
        var proto = new GridFightEnterBattleStageScRsp();
        if (inst == null)
            proto.Retcode = (uint)Retcode.RetFail;
        else
            proto.BattleInfo = inst.ToProto();

        SetData(proto);
    }
}
