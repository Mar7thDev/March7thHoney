using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightBuyGoodsCsReq)]
public class HandlerGridFightBuyGoodsCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightBuyGoodsCsReq.Parser.ParseFrom(data);

        var gridFight = connection.Player?.GridFightManager?.GridFightInstance;
        if (gridFight == null)
        {
            await connection.SendPacket(new PacketGridFightBuyGoodsScRsp(Retcode.RetGridFightNotInGameplay));
            return;
        }

        var shopComp = gridFight.GetComponent<GridFightShopComponent>();
        var code = await shopComp.BuyGoods(req.BuyGoodsIndexList.ToList());

        await connection.SendPacket(new PacketGridFightBuyGoodsScRsp(code));
    }
}