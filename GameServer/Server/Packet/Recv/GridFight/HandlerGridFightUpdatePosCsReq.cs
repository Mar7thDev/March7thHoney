using CastoriceHoney.Server.GameServer.Game.GridFight.Component;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightUpdatePosCsReq)]
public class HandlerGridFightUpdatePosCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightUpdatePosCsReq.Parser.ParseFrom(data);

        if (connection.Player!.GridFightManager!.GridFightInstance == null)
        {
            await connection.SendPacket(
                new PacketGridFightUpdatePosScRsp(Retcode.RetGridFightNotInGameplay, req.GridFightPosInfoList));
            return;
        }

        var gridFight = connection.Player.GridFightManager.GridFightInstance;
        var ret = await gridFight.GetComponent<GridFightRoleComponent>().UpdatePos(req.GridFightPosInfoList.ToList());

        await connection.SendPacket(
            new PacketGridFightUpdatePosScRsp(ret, req.GridFightPosInfoList));
    }
}