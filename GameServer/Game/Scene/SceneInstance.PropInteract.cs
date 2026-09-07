using March7thHoney.Data;
using March7thHoney.Data.Config.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;

namespace March7thHoney.GameServer.Game.Scene;

public partial class SceneInstance
{
    private const int PropInteractFanOutMaxDepth = 8;

    /// <summary>
    ///     Runs the server-side half of the interacted prop's InitLevelGraph: the PropReqInteract edges its
    ///     PropStateExecute blocks declare (<see cref="PropInfo.InteractLinks" />).
    /// </summary>
    /// <remarks>
    ///     Semantics, derived from the robot treasure-hunt graphs and the official wire (floor 20111001 group
    ///     47: the scan trigger's interact reveals chest 60102 Hidden → ChestClosed in the same refresh):
    ///     guards evaluate against the CURRENT scene state - including states this very pass just set - the
    ///     interacted prop is never a target, a target already at the interact's TargetState is skipped, and
    ///     each changed prop's own graph is re-driven recursively with a seen-set against config cycles.
    ///     Returns the props whose state changed, excluding the interacted one.
    /// </remarks>
    public async ValueTask<List<EntityProp>> ApplyPropInteractFanOut(EntityProp interacted)
    {
        var changed = new List<EntityProp>();
        if (interacted.PropInfo.InteractLinks.Count == 0) return changed;

        var seen = new HashSet<(int GroupId, int InstId)> { (interacted.GroupId, interacted.InstId) };
        await FanOutFrom(interacted, seen, changed, 0);
        return changed;
    }

    private async ValueTask FanOutFrom(EntityProp from, HashSet<(int GroupId, int InstId)> seen,
        List<EntityProp> changed, int depth)
    {
        if (depth > PropInteractFanOutMaxDepth) return;

        foreach (var link in from.PropInfo.InteractLinks)
        {
            var key = (link.GroupId, link.InstId);
            if (seen.Contains(key)) continue;
            if (!GuardsHold(link.Guards)) continue;
            if (!GameData.InteractConfigData.TryGetValue(link.InteractId, out var interact)) continue;

            var target = FindProp(link.GroupId, link.InstId);
            if (target == null || target.State == interact.TargetState) continue;

            await target.SetState(interact.TargetState);
            seen.Add(key);
            changed.Add(target);

            // The robot chain continues through the target's own graph (300007 → 300001 → 300004).
            await FanOutFrom(target, seen, changed, depth + 1);
        }
    }

    private bool GuardsHold(List<PropInteractGuard> guards)
    {
        foreach (var guard in guards)
        {
            var prop = FindProp(guard.GroupId, guard.InstId);
            if (prop == null || prop.State != guard.State) return false;
        }

        return true;
    }

    private EntityProp? FindProp(int groupId, int instId)
    {
        foreach (var entity in Entities.Values)
            if (entity is EntityProp prop && prop.GroupId == groupId && prop.InstId == instId)
                return prop;
        return null;
    }
}
