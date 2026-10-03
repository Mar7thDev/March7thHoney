using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Scene;

// The client echoes the position version of the scene it adopted; remembered so a room change can advance it.
[Opcode(CmdIds.SceneUpdatePositionVersionNotify)]
public class HandlerSceneUpdatePositionVersionNotify : Handler<SceneUpdatePositionVersionNotify>
{
    protected override Task OnHandle(Connection connection, PlayerInstance player, SceneUpdatePositionVersionNotify req)
    {
        if (req.PLNEOOCPFNI > 0) player.ClientPosVersion = (int)req.PLNEOOCPFNI;
        return Task.CompletedTask;
    }
}
