using CastoriceHoney.Server.GameServer.Game.Scene.Component;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.EraFlipper;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.EraFlipper;

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