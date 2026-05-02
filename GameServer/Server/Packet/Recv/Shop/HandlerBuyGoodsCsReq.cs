using CastoriceHoney.Server.GameServer.Server.Packet.Send.Shop;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Shop;

[Opcode(CmdIds.BuyGoodsCsReq)]
public class HandlerBuyGoodsCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var player = connection.Player!;
        var req = BuyGoodsCsReq.Parser.ParseFrom(data);
        var items = await player.ShopService!.BuyItem((int)req.ShopId, (int)req.GoodsId, (int)req.GoodsNum);

        await connection.SendPacket(new PacketBuyGoodsScRsp(req, items));
    }
}