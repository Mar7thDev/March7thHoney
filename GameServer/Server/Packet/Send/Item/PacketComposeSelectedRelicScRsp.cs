using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Item;

public class PacketComposeSelectedRelicScRsp : BasePacket
{
    public PacketComposeSelectedRelicScRsp(uint composeId) : base(CmdIds.ComposeSelectedRelicScRsp)
    {
        var proto = new ComposeSelectedRelicScRsp
        {
            ComposeId = composeId,
            Retcode = 1
        };

        SetData(proto);
    }

    public PacketComposeSelectedRelicScRsp(uint composeId, Retcode retcode) : base(CmdIds.ComposeSelectedRelicScRsp)
    {
        var proto = new ComposeSelectedRelicScRsp
        {
            ComposeId = composeId,
            Retcode = (uint)retcode
        };

        SetData(proto);
    }

    public PacketComposeSelectedRelicScRsp(uint composeId, ItemData item)
        : base(CmdIds.ComposeSelectedRelicScRsp)
    {
        var proto = new ComposeSelectedRelicScRsp
        {
            ReturnItemList = new ItemList
            {
                ItemList_ = { item.ToProto() }
            },
            ComposeId = composeId
        };

        SetData(proto);
    }
}