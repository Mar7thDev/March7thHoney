using March7thHoney.GameServer.Game.Battle;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightEnterBattleStageScRsp : BasePacket
{
    public PacketGridFightEnterBattleStageScRsp(Retcode code = Retcode.RetSucc, BattleInstance? inst = null) : base(
        CmdIds.GridFightEnterBattleStageScRsp)
    {
        var proto = new GridFightEnterBattleStageScRsp
        {
            Retcode = (uint)code
        };

        if (inst != null)
            proto.BattleInfo = inst.ToProto();

        SetData(proto);
    }
}