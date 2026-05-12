using March7thHoney.GameServer.Server.Packet.Send.Item;
using March7thHoney.GameServer.Server.Packet.Send.PlayerSync;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Item;

[Opcode(CmdIds.ExchangeHcoinCsReq)]
public class HandlerExchangeHcoinCsReq : Handler
{
    private const int ExchangeRate = 10000; // 1 Hcoin -> 10000 Scoin
    private const int ExchangeMaterialItemId = 201; // 扣除等量原材料

    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = ExchangeHcoinCsReq.Parser.ParseFrom(data);
        var player = connection.Player;
        if (player?.InventoryManager == null) return;

        if (req.Num == 0)
        {
            await connection.SendPacket(new PacketExchangeHcoinScRsp(0, Retcode.RetHcoinExchangeTooMuch));
            return;
        }

        if (player.Data.Hcoin < req.Num)
        {
            await connection.SendPacket(new PacketExchangeHcoinScRsp(req.Num, Retcode.RetItemNotEnough));
            return;
        }

        var material = player.InventoryManager.GetItem(ExchangeMaterialItemId);
        if (material == null || material.Count < req.Num)
        {
            await connection.SendPacket(new PacketExchangeHcoinScRsp(req.Num, Retcode.RetItemNotEnough));
            return;
        }

        await player.InventoryManager.RemoveItem(1, (int)req.Num, sync: false);
        await player.InventoryManager.RemoveItem(ExchangeMaterialItemId, (int)req.Num, sync: false);
        await player.InventoryManager.AddItem(2, (int)req.Num * ExchangeRate, notify: false, sync: false);
        await player.SendPacket(new PacketPlayerSyncScNotify(player.ToProto()));

        await connection.SendPacket(new PacketExchangeHcoinScRsp(req.Num));
    }
}
