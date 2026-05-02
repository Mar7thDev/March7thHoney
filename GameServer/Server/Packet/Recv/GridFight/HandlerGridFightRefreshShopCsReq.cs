using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

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