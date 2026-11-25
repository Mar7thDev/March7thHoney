using March7thHoney.GameServer.Game.Scene.Component;
using March7thHoney.GameServer.Server.Packet.Send.MapRotation;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.MapRotation;

[Opcode(CmdIds.DeployRotatorCsReq)]
public class HandlerDeployRotatorCsReq : Handler<DeployRotatorCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, DeployRotatorCsReq req)
    {
        if (player.SceneInstance == null)
        {
            await connection.SendPacket(new PacketDeployRotatorScRsp(Retcode.RetFail));
            return;
        }

        // The request's only field is the RotaterData of the volume to deploy.
        var rotater = req.JJBDGCGPPFI;
        var region = RotatableRegionSceneComponent.Of(player.SceneInstance);
        if (!region.Deploy((int)(rotater?.GroupId ?? 0), (int)(rotater?.InstanceId ?? 0)))
        {
            await connection.SendPacket(new PacketDeployRotatorScRsp(Retcode.RetFail));
            return;
        }

        await connection.SendPacket(new PacketDeployRotatorScRsp(rotater, region.Energy,
            RotatableRegionSceneComponent.MaxEnergy));
    }
}
