using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Offering;

public class PacketTakeOfferingRewardScRsp : BasePacket
{
    public PacketTakeOfferingRewardScRsp(Retcode ret, OfferingTypeData? data, List<ItemData> reward) : base(
        CmdIds.TakeOfferingRewardScRsp)
    {
        var proto = new TakeOfferingRewardScRsp
        {
            Retcode = (uint)ret
        };

        if (data != null)
        {
            proto.OfferingInfo = data.ToProto();
            proto.Reward = new ItemList
            {
                ItemList_ = { reward.Select(x => x.ToProto()) }
            };
        }

        SetData(proto);
    }
}