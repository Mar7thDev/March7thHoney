using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightBuyExpCsReq)]
public class HandlerGridFightBuyExpCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var gridFight = connection.Player?.GridFightManager?.GridFightInstance;
        if (gridFight == null)
        {
            await connection.SendPacket(CmdIds.GridFightBuyExpScRsp);
            return;
        }

        var basicComp = gridFight.GetComponent<GridFightBasicComponent>();
        await basicComp.BuyLevelExp();

        await connection.SendPacket(CmdIds.GridFightBuyExpScRsp);
    }
}