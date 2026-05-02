using CastoriceHoney.Server.Enums.Item;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.Item;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Item;

[Opcode(CmdIds.DiscardRelicCsReq)]
public class HandlerDiscardRelicCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = DiscardRelicCsReq.Parser.ParseFrom(data);
        var result =
            await connection.Player!.InventoryManager!.DiscardItems(req.RelicIds, req.Discarded,
                ItemMainTypeEnum.Relic);
        await connection.SendPacket(new PacketDiscardRelicScRsp(result, req.Discarded));
    }
}

