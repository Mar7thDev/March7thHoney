using CastoriceHoney.Server.GameServer.Game.Battle;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;

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