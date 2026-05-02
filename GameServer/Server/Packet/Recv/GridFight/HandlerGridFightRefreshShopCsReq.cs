using CastoriceHoney.Server.GameServer.Game.GridFight.Component;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightRefreshShopCsReq)]
public class HandlerGridFightRefreshShopCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var gridFight = connection.Player?.GridFightManager?.GridFightInstance;
        if (gridFight == null)
        {
            await connection.SendPacket(CmdIds.GridFightRefreshShopScRsp);
            return;
        }

        var shopComp = gridFight.GetComponent<GridFightShopComponent>();
        await shopComp.RefreshShop(false);

        await connection.SendPacket(CmdIds.GridFightRefreshShopScRsp);
    }
}