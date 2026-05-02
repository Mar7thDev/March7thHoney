using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;

public class PacketSceneCastSkillCostMpScRsp : BasePacket
{
    public PacketSceneCastSkillCostMpScRsp(int entityId) : base(CmdIds.SceneCastSkillCostMpScRsp)
    {
        var proto = new SceneCastSkillCostMpScRsp
        {
            CastEntityId = (uint)entityId
        };

        SetData(proto);
    }
}