using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Scene;

[Opcode(CmdIds.EnterSceneCsReq)]
public class HandlerEnterSceneCsReq : Handler
{
    // TODO 4.3: social play / TrainCakeCatch 分支暂停，只保留默认场景进入路径
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = EnterSceneCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var storyLineId = (int)(req.SceneIdentifier?.GameStoryLineId ?? 0);

        var teleportId = req.TeleportId != 0 ? (int)req.TeleportId : 0;
        var sceneEntryId = (int)req.EntryId;

        var overMapTp = await player.EnterScene(sceneEntryId, teleportId, true,
            storyLineId, req.IsCloseMap);
        await connection.SendPacket(new PacketEnterSceneScRsp(overMapTp, req.IsCloseMap, storyLineId,
            (uint)player.Data.FloorId));

        await player.TrySendWelcomeAnnounce();
    }
}
