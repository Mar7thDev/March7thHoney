using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Item;

public class PacketSellItemScRsp : BasePacket
{
    public PacketSellItemScRsp(List<ItemData> items) : base(CmdIds.SellItemScRsp)
    {
        var proto = new SellItemScRsp
        {
            ReturnItemList = new ItemList
            {
                ItemList_ = { items.Select(x => x.ToProto()) }
            }
        };

        SetData(proto);
    }
}