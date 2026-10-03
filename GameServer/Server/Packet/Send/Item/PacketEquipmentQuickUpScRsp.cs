using March7thHoney.Database.Inventory;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Item;

public class PacketEquipmentQuickUpScRsp : BasePacket
{
    public PacketEquipmentQuickUpScRsp(Retcode retcode, List<ItemData> returnItems) : base(CmdIds.EquipmentQuickUpScRsp)
    {
        var proto = new EquipmentQuickUpScRsp { Retcode = (uint)retcode };
        proto.ReturnItemList.AddRange(returnItems.Select(item => item.ToPileProto()));
        SetData(proto);
    }
}
