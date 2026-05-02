using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Offering;

public class PacketOfferingInfoScNotify : BasePacket
{
    public PacketOfferingInfoScNotify(OfferingTypeData data) : base(CmdIds.OfferingInfoScNotify)
    {
        var proto = new OfferingInfoScNotify
        {
            OfferingInfo = data.ToProto()
        };

        SetData(proto);
    }
}