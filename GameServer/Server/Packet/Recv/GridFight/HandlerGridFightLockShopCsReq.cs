using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightLockShopCsReq)]
public class HandlerGridFightLockShopCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightLockShopCsReq.Parser.ParseFrom(data);

        var gridFight = connection.Player?.GridFightManager?.GridFightInstance;
        if (gridFight == null)
        {
            await connection.SendPacket(CmdIds.GridFightRefreshShopScRsp);
            return;
        }

        var shopComp = gridFight.GetComponent<GridFightShopComponent>();
        await shopComp.LockGoods(req.IsProtected);

        await connection.SendPacket(CmdIds.GridFightRefreshShopScRsp);
    }
}