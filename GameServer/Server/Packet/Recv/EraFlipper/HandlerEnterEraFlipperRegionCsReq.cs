using March7thHoney.GameServer.Game.Scene.Component;
using March7thHoney.GameServer.Server.Packet.Send.EraFlipper;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.EraFlipper;

[Opcode(CmdIds.EnterEraFlipperRegionCsReq)]
public class HandlerEnterEraFlipperRegionCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = EnterEraFlipperRegionCsReq.Parser.ParseFrom(data);

        var component = connection.Player!.SceneInstance!.GetComponent<EraFlipperSceneComponent>();
        if (component == null)
        {
            await connection.SendPacket(new PacketEnterEraFlipperRegionScRsp(Retcode.RetAdventureMapNotExist));
            return;
        }

        component.EnterEraFlipperRegion((int)req.EraFlipperRegionId, (int)req.State);

        await connection.SendPacket(new PacketEnterEraFlipperRegionScRsp(req.EraFlipperRegionId));
    }
}