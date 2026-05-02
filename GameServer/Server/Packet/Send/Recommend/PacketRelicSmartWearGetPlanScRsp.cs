using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Recommend;

public class PacketRelicSmartWearGetPlanScRsp : BasePacket
{
    public PacketRelicSmartWearGetPlanScRsp(uint avatarId, List<RelicSmartWearPlan> relicPlan)
        : base(CmdIds.RelicSmartWearGetPlanScRsp)
    {
        var proto = new RelicSmartWearGetPlanScRsp
        {
            AvatarId = avatarId,
            RelicPlanList = { relicPlan }
        };

        SetData(proto);
    }
}