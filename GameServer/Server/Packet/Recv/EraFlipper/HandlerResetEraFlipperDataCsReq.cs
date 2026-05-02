using March7thHoney.GameServer.Game.Scene.Component;
using March7thHoney.GameServer.Server.Packet.Send.EraFlipper;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.EraFlipper;

[Opcode(CmdIds.ResetEraFlipperDataCsReq)]
public class HandlerResetEraFlipperDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = ResetEraFlipperDataCsReq.Parser.ParseFrom(data);

        var component = connection.Player!.SceneInstance!.GetComponent<EraFlipperSceneComponent>();
        if (component == null)
        {
            await connection.SendPacket(new PacketResetEraFlipperDataScRsp(Retcode.RetAdventureMapNotExist));
            return;
        }

        // leave
        await connection.SendPacket(
            new PacketResetEraFlipperDataScRsp(component.CurRegionId, component.RegionState, req.PAHMAGPFDDJ));

        component.LeaveFlipperRegion();
    }
}