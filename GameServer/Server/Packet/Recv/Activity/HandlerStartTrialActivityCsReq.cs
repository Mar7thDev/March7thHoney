using March7thHoney.GameServer.Game.Activity.Activities;
using March7thHoney.GameServer.Server.Packet.Send.Activity;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity;

[Opcode(CmdIds.StartTrialActivityCsReq)]
public class HandlerStartTrialActivityCsReq : Handler<StartTrialActivityCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, StartTrialActivityCsReq req)
    {
        var activityManager = player.ActivityManager!;
        // Reuse the running instance so a rejected request never tears down a trial in progress.
        var instance = activityManager.TrialActivityInstance ?? new TrialActivityInstance(activityManager);
        var retcode = await instance.StartActivity((int)req.StageId);
        await connection.SendPacket(retcode == Retcode.RetSucc
            ? new PacketStartTrialActivityScRsp(req.StageId)
            : new PacketStartTrialActivityScRsp(req.StageId, retcode));
    }
}
