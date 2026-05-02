using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Item;

public class PacketComposeItemScRsp : BasePacket
{
    public PacketComposeItemScRsp() : base(CmdIds.ComposeItemScRsp)
    {
        var proto = new ComposeItemScRsp
        {
            Retcode = 1
        };

        SetData(proto);
    }

    public PacketComposeItemScRsp(uint composeId, uint count, ItemData item) : base(CmdIds.ComposeItemScRsp)
    {
        var proto = new ComposeItemScRsp
        {
            ReturnItemList = new ItemList
            {
                ItemList_ = { item.ToProto() }
            },
            ComposeId = composeId,
            Count = count
        };

        SetData(proto);
    }
}