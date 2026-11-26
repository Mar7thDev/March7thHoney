using March7thHoney.GameServer.Server.Packet.Send.MapRotation;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.MapRotation;

[Opcode(CmdIds.RotateMapCsReq)]
public class HandlerRotateMapCsReq : Handler<RotateMapCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, RotateMapCsReq req)
    {
        await connection.SendPacket(new PacketRotateMapScRsp(req.Motion, player.SceneInstance?.ClientPosVersion ?? 0));
    }
}
