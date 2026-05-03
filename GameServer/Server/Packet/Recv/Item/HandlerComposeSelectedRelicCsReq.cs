using March7thHoney.GameServer.Server.Packet.Send.Item;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Server.Packet.Recv.Item;

[Opcode(CmdIds.ComposeSelectedRelicCsReq)]
public class HandlerComposeSelectedRelicCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = ComposeSelectedRelicCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        if (player.InventoryManager!.Data.RelicItems.Count >= GameConstants.INVENTORY_MAX_RELIC)
        {
            await connection.SendPacket(
                new PacketComposeSelectedRelicScRsp(req.ComposeId, Retcode.RetRelicExceedLimit));
            return;
        }

        var item = await player.InventoryManager.ComposeRelic(req);
        if (item == null)
        {
            await connection.SendPacket(new PacketComposeSelectedRelicScRsp(req.ComposeId));
            return;
        }

        await connection.SendPacket(new PacketComposeSelectedRelicScRsp(req.ComposeId, item));
    }
}
