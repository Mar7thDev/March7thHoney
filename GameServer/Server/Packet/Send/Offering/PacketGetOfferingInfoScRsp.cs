using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Offering;

public class PacketGetOfferingInfoScRsp : BasePacket
{
    public PacketGetOfferingInfoScRsp(List<OfferingTypeData> dataList) : base(CmdIds.GetOfferingInfoScRsp)
    {
        var proto = new GetOfferingInfoScRsp
        {
            OfferingInfoList = { dataList.Select(data => data.ToProto()).ToList() }
        };

        SetData(proto);
    }
}