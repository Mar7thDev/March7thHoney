using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.ExpeditionBattlePassStageCnt)]
public class MissionHandlerExpeditionBattlePassStageCnt : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        await ValueTask.CompletedTask;
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        var stars = player.ExpeditionBattleManager?.GetMapStars(excel.ParamInt1) ?? 0;
        await player.QuestManager!.UpdateQuestProgress(quest.QuestID, stars);
    }
}
