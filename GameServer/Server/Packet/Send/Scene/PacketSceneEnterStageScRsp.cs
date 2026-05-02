using CastoriceHoney.Server.GameServer.Game.Battle;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;

public class PacketSceneEnterStageScRsp : BasePacket
{
    public PacketSceneEnterStageScRsp() : base(CmdIds.SceneEnterStageScRsp)
    {
        var proto = new SceneEnterStageScRsp
        {
            Retcode = 1
        };

        SetData(proto);
    }

    public PacketSceneEnterStageScRsp(BattleInstance battleInstance) : base(CmdIds.SceneEnterStageScRsp)
    {
        var proto = new SceneEnterStageScRsp
        {
            BattleInfo = battleInstance.ToProto()
        };

        SetData(proto);
    }
}