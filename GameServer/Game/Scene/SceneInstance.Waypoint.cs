using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.Scene;

public partial class SceneInstance
{
    /// <summary>Squared 2D range (same unit as the spring heal) within which a waypoint counts as reached.</summary>
    public const long WaypointReachRangeSq = 25_000_000;

    /// <summary>
    ///     Official waypoint unlock: the first time the player reaches a teleport checkpoint (Space Anchor,
    ///     healing spring, Calyx/cocoon - any prop carrying MappingInfoID) the server flips it
    ///     CheckPointDisable → CheckPointEnable (SceneGroupRefreshScNotify) and follows with
    ///     UnlockTeleportNotify{teleport_id, entry_id}; from then on the map lists it as unlocked.
    /// </summary>
    /// <remarks>
    ///     Official captures: 4.4 part2.json idx 535 (spring 300007 → state 8 on arrival), part4.1.json idx
    ///     9652-9656 (cocoon prop 808 → CheckPointEnable + UnlockTeleportNotify{1001, 2010101}), 4.5
    ///     part5.1.json idx 7474-7478. Runs on every position update and on map-teleport arrival. A checkpoint
    ///     that is already CheckPointEnable by config (most springs) is only RECORDED as unlocked - the official
    ///     server ships an empty unlock list until the player actually reaches even those.
    ///     Returns the prop whose unlock was newly recorded, or null.
    /// </remarks>
    public async ValueTask<EntityProp?> TryUnlockNearbyWaypoint(Position? pos)
    {
        if (pos == null) return null;

        EntityProp? nearest = null;
        long nearestDist = long.MaxValue;
        foreach (var entity in Entities.Values)
        {
            if (entity is not EntityProp prop || prop.PropInfo.MappingInfoID == 0) continue;
            if (prop.State is not (PropStateEnum.CheckPointDisable or PropStateEnum.CheckPointEnable)) continue;
            if (prop.State == PropStateEnum.CheckPointEnable &&
                Player.IsTeleportUnlocked(FloorId, prop.PropInfo.MappingInfoID)) continue;

            var dist = pos.GetFast2dDist(prop.Position);
            if (dist > WaypointReachRangeSq || dist >= nearestDist) continue;
            nearest = prop;
            nearestDist = dist;
        }

        if (nearest == null) return null;

        if (nearest.State == PropStateEnum.CheckPointDisable)
        {
            // SetState refreshes the entity, persists the prop state and records + notifies the unlock.
            await nearest.SetState(PropStateEnum.CheckPointEnable, true);
            // A PropState sub-mission gated on this checkpoint (ParamInt3 = CheckPointEnable) can now finish.
            if (Player.MissionManager != null) await Player.MissionManager.OnPlayerInteractWithProp();
        }
        else
        {
            await Player.RecordTeleportUnlock(FloorId, nearest.PropInfo.MappingInfoID, true);
        }

        return nearest;
    }
}
