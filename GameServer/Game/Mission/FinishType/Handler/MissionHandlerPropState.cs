using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.Enums.Scene;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.GameServer.Game.Scene.Entity;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.PropState)]
public class MissionHandlerPropState : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (player.SceneInstance?.FloorId != info.LevelFloorID) return; // not a same scene
        var prop = player.SceneInstance.GetEntitiesInGroup<EntityProp>(info.ParamInt1);

        foreach (var p in prop)
            if (p.PropInfo.ID == info.ParamInt2 && (int)p.State == info.ParamInt3)
                await player.MissionManager!.FinishSubMission(info.ID);
            else if (info.ParamInt3 == (int)PropStateEnum.CheckPointDisable ||
                     info.ParamInt3 == (int)PropStateEnum.CheckPointEnable)
                await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        if (player.SceneInstance?.FloorId != excel.MazeFloorID) return; // not a same scene
        var prop = player.SceneInstance.GetEntitiesInGroup<EntityProp>(excel.ParamInt1);

        foreach (var p in prop)
            if (p.PropInfo.ID == excel.ParamInt2 && (int)p.State == excel.ParamInt3)
                await player.QuestManager!.AddQuestProgress(quest.QuestID, 1);
            else if (excel.ParamInt3 == (int)PropStateEnum.CheckPointDisable ||
                     excel.ParamInt3 == (int)PropStateEnum.CheckPointEnable)
                await player.QuestManager!.AddQuestProgress(quest.QuestID, 1);
    }
}