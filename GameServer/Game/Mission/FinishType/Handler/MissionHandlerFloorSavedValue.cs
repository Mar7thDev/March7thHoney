using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.FloorSavedValue)]
public class MissionHandlerFloorSavedValue : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (player.SceneData?.FloorSavedData.TryGetValue(info.LevelFloorID, out var floor) == true)
            if (floor.TryGetValue(info.ParamStr1, out var value))
                if (value == info.ParamInt1)
                    await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        // this type wont be used in quest
        await ValueTask.CompletedTask;
    }
}