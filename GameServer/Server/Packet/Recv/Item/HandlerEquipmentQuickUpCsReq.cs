using March7thHoney.GameServer.Server.Packet.Send.Item;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Item;

[Opcode(CmdIds.EquipmentQuickUpCsReq)]
public class HandlerEquipmentQuickUpCsReq : Handler<EquipmentQuickUpCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, EquipmentQuickUpCsReq req)
    {
        var (retcode, items) = await player.InventoryManager!.QuickLevelUpEquipment(req);
        await connection.SendPacket(new PacketEquipmentQuickUpScRsp(retcode, items));
    }
}
