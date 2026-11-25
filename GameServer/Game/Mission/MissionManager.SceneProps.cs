using March7thHoney.Data;
using March7thHoney.Data.Config;
using March7thHoney.Data.Config.Scene;
using March7thHoney.Enums.Mission;
using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;

namespace March7thHoney.GameServer.Game.Mission;

public partial class MissionManager
{
    /// <summary>
    ///     Places a finished PropState sub's target prop (ParamInt1 group, ParamInt2 inst) on the state the sub waited
    ///     for (ParamInt3): the server half of the sub graph's bare ChangePropState, whose parameters the dump strips.
    /// </summary>
    /// <remarks>
    ///     Official 4.7: consuming the jade seal finishes 102110126 and 102110131 together, and the refresh that
    ///     follows opens floor 20222001 g10/300001 (the corridor Door_01 no interaction ever touches). A prop that is
    ///     already there - the usual case, since the sub's gate watches it - is left alone. A prop outside the live
    ///     scene is saved for the next build of the floor the sub names. Checkpoint subs are skipped: their handler
    ///     finishes them without the checkpoint being reached, and forcing it would unlock the waypoint.
    /// </remarks>
    private async ValueTask ApplyPropStateFinishEffect(SubMissionInfo? info)
    {
        if (info is not { FinishType: MissionFinishTypeEnum.PropState, ParamInt1: not 0, ParamInt2: not 0 }) return;
        var target = (PropStateEnum)info.ParamInt3;
        if (!Enum.IsDefined(target) ||
            target is PropStateEnum.CheckPointDisable or PropStateEnum.CheckPointEnable) return;

        var scene = Player.SceneInstance;
        var floorId = info.LevelFloorID != 0 ? info.LevelFloorID : scene?.FloorId ?? 0;
        if (floorId == 0) return;

        if (scene != null && scene.FloorId == floorId)
        {
            var prop = scene.GetEntitiesInGroup<EntityProp>(info.ParamInt1)
                .FirstOrDefault(p => p.InstId == info.ParamInt2 && !p.IsGmSpawned);
            if (prop != null)
            {
                if (prop.State != target) await prop.SetState(target);
                return;
            }

            if (!scene.PersistsPropState) return;
        }

        var planeId = info.LevelPlaneID;
        if (planeId == 0 && scene?.FloorId == floorId) planeId = scene.PlaneId;
        if (planeId == 0)
            planeId = GameData.MapEntranceData.Values.FirstOrDefault(entrance => entrance.FloorID == floorId)
                ?.PlaneID ?? 0;
        if (!GameData.MazePlaneData.TryGetValue(planeId, out var plane) ||
            plane.PlaneType is PlaneTypeEnum.Raid or PlaneTypeEnum.Rogue) return;

        GameData.GetFloorInfo(planeId, floorId, out var floorInfo);
        FloorInfo? floor = floorInfo;
        if (floor == null || !floor.Groups.TryGetValue(info.ParamInt1, out var group) ||
            group.SaveType == SaveTypeEnum.Reset) return;

        if (Player.GetScenePropData(floorId, info.ParamInt1, info.ParamInt2)?.State == target) return;
        Player.SetScenePropData(floorId, info.ParamInt1, info.ParamInt2, target);
    }
}
