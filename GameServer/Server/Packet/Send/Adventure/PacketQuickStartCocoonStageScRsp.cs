using CastoriceHoney.Server.GameServer.Game.Battle;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Adventure;

public class PacketQuickStartCocoonStageScRsp : BasePacket
{
    public PacketQuickStartCocoonStageScRsp() : base(CmdIds.QuickStartCocoonStageScRsp)
    {
        var rsp = new QuickStartCocoonStageScRsp
        {
            Retcode = 1
        };

        SetData(rsp);
    }

    public PacketQuickStartCocoonStageScRsp(BattleInstance battle, int cocoonId, int wave) : base(
        CmdIds.QuickStartCocoonStageScRsp)
    {
        var rsp = new QuickStartCocoonStageScRsp
        {
            CocoonId = (uint)cocoonId,
            Wave = (uint)wave,
            Count = (uint)wave,
            BattleInfo = battle.ToProto()
        };

        SetData(rsp);
    }
}

