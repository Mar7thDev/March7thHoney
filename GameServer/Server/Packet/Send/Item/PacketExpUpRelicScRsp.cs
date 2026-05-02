using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Item;

public class PacketExpUpRelicScRsp : BasePacket
{
    public PacketExpUpRelicScRsp(List<ItemData> leftover) : base(CmdIds.ExpUpRelicScRsp)
    {
        var proto = new ExpUpRelicScRsp();

        foreach (var item in leftover) proto.ReturnItemList.Add(item.ToPileProto());

        SetData(proto);
    }
}