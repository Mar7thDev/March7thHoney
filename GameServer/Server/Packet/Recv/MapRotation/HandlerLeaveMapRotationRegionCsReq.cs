using March7thHoney.GameServer.Game.Scene.Component;
using March7thHoney.GameServer.Server.Packet.Send.MapRotation;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.MapRotation;

[Opcode(CmdIds.LeaveMapRotationRegionCsReq)]
public class HandlerLeaveMapRotationRegionCsReq : Handler<LeaveMapRotationRegionCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player,
        LeaveMapRotationRegionCsReq req)
    {
        player.SceneInstance?.GetComponent<RotatableRegionSceneComponent>()?.Leave();
        await connection.SendPacket(new PacketLeaveMapRotationRegionScRsp(req.Motion,
            player.SceneInstance?.ClientPosVersion ?? 0));
    }
}
