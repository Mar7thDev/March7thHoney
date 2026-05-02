using March7thHoney.GameServer.Game.RogueTourn.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.GameServer.Server.Packet.Send.RogueCommon;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.RogueCommon;

[Opcode(CmdIds.RogueWorkbenchGetInfoCsReq)]
public class HandlerRogueWorkbenchGetInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RogueWorkbenchGetInfoCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;

        BaseGameEntity? entity = null;
        player.SceneInstance?.Entities.TryGetValue((int)req.PropEntityId, out entity);
        if (entity is not RogueWorkbenchProp prop)
        {
            await connection.SendPacket(new PacketRogueWorkbenchGetInfoScRsp(Retcode.RetSceneEntityNotExist, null));
            return;
        }

        await connection.SendPacket(new PacketRogueWorkbenchGetInfoScRsp(Retcode.RetSucc, prop));
    }
}