using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.GroupPropertyValue)]
public class MissionHandlerGroupPropertyValue : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        var floorId = info.LevelFloorID;
        var groupId = info.ParamInt1;
        var value = info.ParamInt2;
        var name = info.ParamStr1;

        if (player.SceneInstance?.FloorId != floorId) return;
        var prop = player.SceneInstance.GetGroupProperty(groupId, name);
        if (prop == value) await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        var floorId = excel.MazeFloorID;
        var groupId = excel.ParamInt1;
        var value = excel.ParamInt2;
        var name = excel.ParamStr1;

        if (player.SceneInstance?.FloorId != floorId) return;
        var prop = player.SceneInstance.GetGroupProperty(groupId, name);
        if (prop == value) await player.QuestManager!.AddQuestProgress(quest.QuestID, 1);
    }
}