using CastoriceHoney.Server.GameServer.Server.Packet.Send.Shop;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Shop;

[Opcode(CmdIds.GetShopListCsReq)]
public class HandlerGetShopListCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetShopListCsReq.Parser.ParseFrom(data);

        await connection.SendPacket(new PacketGetShopListScRsp(req.ShopType));
    }
}