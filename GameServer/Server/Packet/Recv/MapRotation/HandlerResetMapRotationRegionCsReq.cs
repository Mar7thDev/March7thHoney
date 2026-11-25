using March7thHoney.GameServer.Game.Scene.Component;
using March7thHoney.GameServer.Server.Packet.Send.MapRotation;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.MapRotation;

[Opcode(CmdIds.ResetMapRotationRegionCsReq)]
public class HandlerResetMapRotationRegionCsReq : Handler<ResetMapRotationRegionCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player,
        ResetMapRotationRegionCsReq req)
    {
        // The response has no energy field; the refilled pool is reported by the next deploy.
        if (player.SceneInstance != null) RotatableRegionSceneComponent.Of(player.SceneInstance).Refill();
        await connection.SendPacket(new PacketResetMapRotationRegionScRsp(req.Motion,
            player.SceneInstance?.ClientPosVersion ?? 0));
    }
}
