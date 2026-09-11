using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.FinalityBattleLevelFinish)]
public class MissionHandlerFinalityBattleLevelFinish : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        await ValueTask.CompletedTask;
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        if (arg is not BattleInstance { FinalityContext: { } context } battle) return;
        if (battle.BattleEndStatus != BattleEndStatus.BattleEndWin || context.Level.ID != excel.ParamInt1) return;
        // ParamInt2 selects a battle target by 1-based index; 0 means plain clear.
        if (excel.ParamInt2 > 0)
        {
            var targets = context.Level.BattleTargetList;
            if (excel.ParamInt2 > targets.Count || !context.FinishedTargetIds.Contains(targets[excel.ParamInt2 - 1])) return;
        }

        await player.QuestManager!.AddQuestProgress(quest.QuestID, 1);
    }
}
