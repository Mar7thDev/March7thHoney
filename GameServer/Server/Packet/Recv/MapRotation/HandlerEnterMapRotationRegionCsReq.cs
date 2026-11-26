using March7thHoney.GameServer.Game.Scene.Component;
using March7thHoney.GameServer.Server.Packet.Send.MapRotation;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.MapRotation;

[Opcode(CmdIds.EnterMapRotationRegionCsReq)]
public class HandlerEnterMapRotationRegionCsReq : Handler<EnterMapRotationRegionCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, EnterMapRotationRegionCsReq req)
    {
        var scene = player.SceneInstance;
        var energy = RotatableRegionSceneComponent.MaxEnergy;
        if (scene != null)
        {
            var region = RotatableRegionSceneComponent.Of(scene);
            region.Enter();
            energy = region.Energy;
        }

        // The official sends the energy notify before the response, so the HUD is filled when the response lands.
        await connection.SendPacket(new PacketUpdateEnergyScNotify(energy, RotatableRegionSceneComponent.MaxEnergy));
        await connection.SendPacket(new PacketEnterMapRotationRegionScRsp(req, scene?.ClientPosVersion ?? 0, energy,
            RotatableRegionSceneComponent.MaxEnergy));
    }
}
