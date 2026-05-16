using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Mission;

[Opcode(CmdIds.StarPerformanceRelayCsReq)]
public class HandlerStarPerformanceRelayCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = StarPerformanceRelayCsReq.Parser.ParseFrom(data);
        var player = connection.Player;
        if (player == null) return;

        // Story/performance relay packets are emitted when scripted sequences end or hand control
        // back to the regular world. Re-evaluate mission/storyline state so the client can exit cleanly.
        if (player.MissionManager != null)
            await player.MissionManager.HandleAllFinishType(req.PerformanceId == 0 ? null : req.PerformanceId);

        if (player.StoryLineManager != null)
        {
            await player.StoryLineManager.CheckIfFinishStoryLine();

            if (player.StoryLineManager.StoryLineData.CurStoryLineId != 0)
                await player.StoryLineManager.LeaveStoryLine(true);
            else
                await player.StoryLineManager.CheckIfEnterStoryLine();
        }

        player.SceneInstance?.SyncGroupInfo();
        if (player.QuestManager != null)
            await player.QuestManager.SyncQuest();
    }
}