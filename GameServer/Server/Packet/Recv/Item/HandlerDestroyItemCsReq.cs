using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Item;

[Opcode(CmdIds.DestroyItemCsReq)]
public class HandlerDestroyItemCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = DestroyItemCsReq.Parser.ParseFrom(data);

        await connection.Player!.InventoryManager!.RemoveItem((int)req.ItemId, (int)req.ItemCount);
        await connection.SendPacket(CmdIds.DestroyItemScRsp);
    }
}