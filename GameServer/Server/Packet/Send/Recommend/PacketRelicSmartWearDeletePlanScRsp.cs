using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Recommend;

public class PacketRelicSmartWearDeletePlanScRsp : BasePacket
{
    public PacketRelicSmartWearDeletePlanScRsp(uint uniqueId)
        : base(CmdIds.RelicSmartWearDeletePlanScRsp)
    {
        var proto = new RelicSmartWearDeletePlanScRsp
        {
            UniqueId = uniqueId
        };

        SetData(proto);
    }
}