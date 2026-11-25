using March7thHoney.GameServer.Game.Scene.Component;
using March7thHoney.GameServer.Server.Packet.Send.MapRotation;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.MapRotation;

[Opcode(CmdIds.InteractChargerCsReq)]
public class HandlerInteractChargerCsReq : Handler<ICBEPDGBHMM>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, ICBEPDGBHMM req)
    {
        // A charger refills the same pool the bridge deploys spend.
        var energy = RotatableRegionSceneComponent.MaxEnergy;
        if (player.SceneInstance != null)
        {
            var region = RotatableRegionSceneComponent.Of(player.SceneInstance);
            region.Refill();
            energy = region.Energy;
        }

        await connection.SendPacket(new PacketInteractChargerScRsp());
        await connection.SendPacket(new PacketUpdateEnergyScNotify(energy, RotatableRegionSceneComponent.MaxEnergy));
    }
}
