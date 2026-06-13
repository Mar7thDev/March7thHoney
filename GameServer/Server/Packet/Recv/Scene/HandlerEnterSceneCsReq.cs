using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Scene;

[Opcode(CmdIds.EnterSceneCsReq)]
public class HandlerEnterSceneCsReq : Handler<EnterSceneCsReq>
{
    // TODO 4.3: social play / TrainCakeCatch 鍒嗘敮鏆傚仠锛屽彧淇濈暀榛樿鍦烘櫙杩涘叆璺緞
    protected override async Task OnHandle(Connection connection, PlayerInstance player, EnterSceneCsReq req)
    {
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
