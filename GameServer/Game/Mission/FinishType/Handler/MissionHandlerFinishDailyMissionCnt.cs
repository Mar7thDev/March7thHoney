using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.DailyActive;
using March7thHoney.GameServer.Game.Player;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>
///     Daily main missions ever finished (cycle quests 1001802 / 1001925).
///     State-based (progress is set to the current count, never incremented), so heartbeats are harmless.
/// </summary>
[MissionFinishType(MissionFinishTypeEnum.FinishDailyMissionCnt)]
public class MissionHandlerFinishDailyMissionCnt : MissionFinishTypeHandler
{
    private static int Count(PlayerInstance player)
    {
        return player.GetComponentOrDefault<DailyActiveManager>()?.TotalMissionCount ?? 0;
    }

    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (Count(player) >= Math.Max(info.Progress, 1)) await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        await player.QuestManager!.UpdateQuestProgress(quest.QuestID, Count(player));
    }
}
