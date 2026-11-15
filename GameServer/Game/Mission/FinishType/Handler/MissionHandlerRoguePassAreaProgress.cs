using March7thHoney.Data;
using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Rogue;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.RoguePassAreaProgress)]
public class MissionHandlerRoguePassAreaProgress : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        // Only a settled winning run can complete this gate; merely entering/leaving is not a clear.
        if (arg is not RogueInstance { IsWin: true, Status: Proto.RogueStatus.Finish } run ||
            !GameData.RogueAreaConfigData.TryGetValue(run.AreaId, out var area) ||
            area.AreaProgress != info.ParamInt1) return;
        await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg) => ValueTask.CompletedTask;
}
