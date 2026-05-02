using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Item;

public class PacketGetBagScRsp : BasePacket
{
    public PacketGetBagScRsp(PlayerInstance player) : base(CmdIds.GetBagScRsp)
    {
        var proto = new GetBagScRsp();

        player.InventoryManager!.Data.MaterialItems.ForEach(item =>
        {
            proto.MaterialList.Add(item.ToMaterialProto());
        });

        player.InventoryManager.Data.RelicItems.ForEach(item => { proto.RelicList.Add(item.ToRelicProto()); });

        player.InventoryManager.Data.EquipmentItems.ForEach(item =>
        {
            proto.EquipmentList.Add(item.ToEquipmentProto());
        });

        SetData(proto);
    }
}