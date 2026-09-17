using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Scene;

[Opcode(CmdIds.GetEnteredSceneCsReq)]
public class HandlerGetEnteredSceneCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketGetEnteredSceneScRsp(player));
    }
}
