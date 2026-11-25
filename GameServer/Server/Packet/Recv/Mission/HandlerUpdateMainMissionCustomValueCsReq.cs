using March7thHoney.GameServer.Server.Packet.Send.Mission;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Mission;

[Opcode(CmdIds.UpdateMainMissionCustomValueCsReq)]
public class HandlerUpdateMainMissionCustomValueCsReq : Handler<UpdateMainMissionCustomValueCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player,
        UpdateMainMissionCustomValueCsReq req)
    {
        var retcode = await player.MissionManager!.UpdateMainMissionCustomValue((int)req.MainMissionId, req.Value,
            req.BGDFHBAANLB ?? "");
        await connection.SendPacket(new PacketUpdateMainMissionCustomValueScRsp(retcode, player, req.MainMissionId));
    }
}
