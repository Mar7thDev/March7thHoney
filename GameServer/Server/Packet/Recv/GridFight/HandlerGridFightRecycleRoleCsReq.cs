using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightRecycleRoleCsReq)]
public class HandlerGridFightRecycleRoleCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightRecycleRoleCsReq.Parser.ParseFrom(data);

        var gridFight = connection.Player?.GridFightManager?.GridFightInstance;
        if (gridFight == null)
        {
            await connection.SendPacket(CmdIds.GridFightRecycleRoleScRsp);
            return;
        }

        var roleComp = gridFight.GetComponent<GridFightRoleComponent>();
        await roleComp.SellAvatar(req.UniqueId);

        await connection.SendPacket(CmdIds.GridFightRecycleRoleScRsp);
    }
}