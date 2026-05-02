using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Item;

public class PacketExpUpEquipmentScRsp : BasePacket
{
    public PacketExpUpEquipmentScRsp(List<ItemData> returnItem) : base(CmdIds.ExpUpEquipmentScRsp)
    {
        var proto = new ExpUpEquipmentScRsp();
        proto.ReturnItemList.AddRange(returnItem.Select(item => item.ToPileProto()));

        SetData(proto);
    }
}