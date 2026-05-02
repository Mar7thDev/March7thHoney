using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.Enums.Scene;
using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.PropTypeInteract)]
public class MissionHandlerPropTypeInteract : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        // this type wont be used in mission
        await ValueTask.CompletedTask;
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        var propCount = 0;
        foreach (var floor in player.SceneData?.ScenePropData ?? [])
        foreach (var group in floor.Value)
        foreach (var prop in group.Value)
            if (prop.State == (PropStateEnum)excel.ParamInt2) // interacted
                propCount++;

        await player.QuestManager!.UpdateQuestProgress(quest.QuestID, propCount);
    }
}