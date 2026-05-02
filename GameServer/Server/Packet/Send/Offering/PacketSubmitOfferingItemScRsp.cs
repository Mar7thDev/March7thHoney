using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Offering;

public class PacketSubmitOfferingItemScRsp : BasePacket
{
    public PacketSubmitOfferingItemScRsp(Retcode ret, OfferingTypeData? data) : base(CmdIds.SubmitOfferingItemScRsp)
    {
        var proto = new SubmitOfferingItemScRsp
        {
            Retcode = (uint)ret
        };

        if (data != null) proto.OfferingInfo = data.ToProto();

        SetData(proto);
    }
}