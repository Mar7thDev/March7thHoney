using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Scene;

[Opcode(CmdIds.SceneEntityTeleportCsReq)]
public class HandlerSceneEntityTeleportCsReq : Handler<SceneEntityTeleportCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, SceneEntityTeleportCsReq req)
    {
        if (req.EntryId != player.Data.EntryId) await player.EnterScene((int)req.EntryId, 0, false);
        player.MoveTo(req.EntityMotion);

        // Arriving at a Space Anchor / spring / Calyx by map teleport unlocks it exactly like walking into range
        // (4.4 part4.1.json idx 9652-9656: refresh prop 808 → CheckPointEnable + UnlockTeleportNotify BEFORE the
        // teleport ScRsp; 4.5 mirror part5.1.json idx 7474-7478).
        if (player.SceneInstance != null) await player.SceneInstance.TryUnlockNearbyWaypoint(player.Data.Pos);

        await connection.SendPacket(new PacketSceneEntityTeleportScRsp(req.EntityMotion));
    }
}
