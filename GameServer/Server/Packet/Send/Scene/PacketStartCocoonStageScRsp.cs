using CastoriceHoney.Server.GameServer.Game.Battle;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;

public class PacketStartCocoonStageScRsp : BasePacket
{
    public PacketStartCocoonStageScRsp() : base(CmdIds.StartCocoonStageScRsp)
    {
        var rsp = new StartCocoonStageScRsp
        {
            Retcode = 1
        };

        SetData(rsp);
    }

    public PacketStartCocoonStageScRsp(BattleInstance battle, int cocoonId, int wave) : base(
        CmdIds.StartCocoonStageScRsp)
    {
        var rsp = new StartCocoonStageScRsp
        {
            CocoonId = (uint)cocoonId,
            Wave = (uint)wave,
            BattleInfo = battle.ToProto()
        };

        SetData(rsp);
    }
}