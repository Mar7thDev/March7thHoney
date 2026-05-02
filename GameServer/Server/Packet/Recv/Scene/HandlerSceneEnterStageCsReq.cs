using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Scene;

[Opcode(CmdIds.SceneEnterStageCsReq)]
public class HandlerSceneEnterStageCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SceneEnterStageCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        await player.BattleManager!.StartStage((int)req.EventId);
    }
}