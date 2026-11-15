using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Rogue;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.RogueEnterRoomWithLevel)]
public class MissionHandlerRogueEnterRoomWithLevel : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (arg is not RogueInstance { Status: Proto.RogueStatus.Doing } run ||
            run.MapId != info.ParamInt1 || run.CurrentSiteId != info.ParamInt2) return;
        await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg) => ValueTask.CompletedTask;
}
