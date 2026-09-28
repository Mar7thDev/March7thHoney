using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.DailyActive;
using March7thHoney.GameServer.Game.Player;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>
///     Daily main missions finished in the current daily window (quest 2100003 "Complete 1 Daily Mission").
///     State-based (progress is set to the current count, never incremented), so heartbeats are harmless.
/// </summary>
[MissionFinishType(MissionFinishTypeEnum.FinishTodayDailyMissionCnt)]
public class MissionHandlerFinishTodayDailyMissionCnt : MissionFinishTypeHandler
{
    private static int Count(PlayerInstance player)
    {
        return player.GetComponentOrDefault<DailyActiveManager>()?.TodayMissionCount ?? 0;
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
