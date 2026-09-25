using March7thHoney.GameServer.Server.Packet.Send.Activity;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity;

[Opcode(CmdIds.LeaveTrialActivityCsReq)]
public class HandlerLeaveTrialActivityCsReq : Handler<LeaveTrialActivityCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, LeaveTrialActivityCsReq req)
    {
        var manager = player.ActivityManager!;
        if (manager.TrialActivityInstance is { Data.CurTrialStageId: not 0 } instance)
            await instance.LeaveActivity();

        manager.TrialActivityInstance = null;

        await connection.SendPacket(new PacketLeaveTrialActivityScRsp(req.StageId));
    }
}
