using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;

public class PacketSceneCastSkillMpUpdateScNotify : BasePacket
{
    public PacketSceneCastSkillMpUpdateScNotify(uint castEntityId, int mpCount) : base(
        CmdIds.SceneCastSkillMpUpdateScNotify)
    {
        var proto = new SceneCastSkillMpUpdateScNotify
        {
            CastEntityId = castEntityId,
            Mp = (uint)mpCount
        };

        SetData(proto);
    }
}