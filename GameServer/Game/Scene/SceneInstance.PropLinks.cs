using March7thHoney.Data;
using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;

namespace March7thHoney.GameServer.Game.Scene;

/// <summary>
///     Prop rules driven by the "group,inst" links a prop names in its ValueSource. The client graphs of the linked
///     props only listen for state changes, so the server has to make them.
/// </summary>
public partial class SceneInstance
{
    /// <summary>
    ///     Opens every minimap hint marker whose watched prop is already finished, and returns the markers it moved.
    /// </summary>
    /// <remarks>
    ///     Official 4.7: a looted chest ships its marker at Open in the same refresh (11 of 11 loots, e.g. floor
    ///     20221001 chest 114/300004 → 13 with marker 114/300005 → 1), and a solved Shifting Screen ships both of its
    ///     markers at Open with the boards at CustomState01. A scene build runs the same check, so a save that
    ///     finished the puzzle before markers were cleared stops showing the pin. Only markers whose own state list
    ///     declares Open are moved.
    /// </remarks>
    public async ValueTask<List<EntityProp>> OpenSolvedMapMarkers(bool sendPacket)
    {
        var opened = new List<EntityProp>();
        foreach (var marker in Entities.Values.OfType<EntityProp>().ToList())
        {
            if (marker.IsGmSpawned || marker.State == PropStateEnum.Open) continue;
            var info = marker.PropInfo;
            if (info.MapMarkerWatchGroupId == 0 || info.MapMarkerWatchInstId == 0) continue;
            if (!marker.Excel.PropStateList.Contains(PropStateEnum.Open)) continue;

            var watched = FindProp(info.MapMarkerWatchGroupId, info.MapMarkerWatchInstId);
            if (watched == null || !CountsAsSolved(watched)) continue;

            await marker.SetState(PropStateEnum.Open, sendPacket);
            opened.Add(marker);
        }

        return opened;
    }

    /// <summary>
    ///     Whether a watched prop is finished: a looted chest; a puzzle board at Open or at a post-solve
    ///     CustomState01..09 (the Shifting Screen lands on 101, and the solved dimension variants author Open); or an
    ///     ordinary console at Open (the InsertionDoor family watches a console, not a board).
    /// </summary>
    private static bool CountsAsSolved(EntityProp prop)
    {
        if (prop.State == PropStateEnum.ChestUsed) return true;
        return prop.Excel.PropType switch
        {
            PropTypeEnum.PROP_MAZE_PUZZLE => prop.State is PropStateEnum.Open or
                (>= PropStateEnum.CustomState01 and <= PropStateEnum.CustomState09),
            PropTypeEnum.PROP_ORDINARY => prop.State == PropStateEnum.Open,
            _ => false
        };
    }

    /// <summary>
    ///     A solved puzzle board (now Open) unlocks the console its ValueSource "LockedConsole" names, Locked → Closed.
    ///     Official 4.7: the coil board press on floor 20222001 45/300002 ships the board Open and console 10/300013
    ///     Closed in one refresh; left Locked, the console only shows "Unable to operate Central Console at the
    ///     moment" and the "Return to the Creation Furnace" objective soft-locks.
    /// </summary>
    public async ValueTask UnlockLinkedConsole(EntityProp puzzle)
    {
        if (puzzle.State != PropStateEnum.Open) return;
        await UnlockLockedConsole(puzzle.PropInfo.LockedConsoleGroupId, puzzle.PropInfo.LockedConsoleInstId, true);
    }

    /// <summary>
    ///     Scene-build twin of <see cref="UnlockLinkedConsole" />. The defect left no saved state on the console, so
    ///     the signal is the board: a live board at Open, or a saved Open board whose group is not loaded any more (the
    ///     coil board's group unloads once its subs finish, so by the time the player reaches the console only its
    ///     saved state remains).
    /// </summary>
    public async ValueTask UnlockSolvedPuzzleConsoles()
    {
        var links = new HashSet<(int GroupId, int InstId)>();
        foreach (var prop in Entities.Values.OfType<EntityProp>())
            if (prop.State == PropStateEnum.Open && prop.PropInfo.LockedConsoleGroupId != 0)
                links.Add((prop.PropInfo.LockedConsoleGroupId, prop.PropInfo.LockedConsoleInstId));

        if (FloorInfo != null && Player.SceneData?.ScenePropData.TryGetValue(FloorId, out var floorProps) == true)
            foreach (var (groupId, savedProps) in floorProps)
            {
                if (!FloorInfo.Groups.TryGetValue(groupId, out var group)) continue;
                foreach (var saved in savedProps.Where(saved => saved.State == PropStateEnum.Open))
                    if (group.PropList.Find(prop => prop.ID == saved.PropId) is { LockedConsoleGroupId: not 0 } info)
                        links.Add((info.LockedConsoleGroupId, info.LockedConsoleInstId));
            }

        foreach (var (groupId, instId) in links)
            await UnlockLockedConsole(groupId, instId, false);
    }

    /// <summary>
    ///     A lift press names the LIFT, never the call console the player pressed. A Chap02 call console holds the
    ///     player in the interact pose until it reaches Open itself ("Cannot redirect"), so the pressed console moves
    ///     to Open and the lift's other call consoles back to Closed, which re-arms them. Official 4.7: press 115140
    ///     on floor 20222001 g51 ships lift → 14, console 300003 → 1 and 300002 → 0; press 114150 the reverse. The
    ///     Chap01 / Chap00 consoles release on their own and are never moved (official 4.5 refreshes only the lift).
    /// </summary>
    public async ValueTask ApplyLiftCallConsoles(EntityProp lift, int interactId)
    {
        if (lift.Excel.PropType != PropTypeEnum.PROP_ELEVATOR || interactId == 0) return;
        foreach (var (console, interactIds) in LiftCallConsoles(lift))
        {
            var target = interactIds.Contains(interactId) ? PropStateEnum.Open : PropStateEnum.Closed;
            if (console.State != target) await console.SetState(target);
        }
    }

    /// <summary>
    ///     The call consoles of one lift: props of the lift's group whose graph releases the interact on Open and
    ///     which request lift-position interacts on that lift, with the interact ids each one sends.
    /// </summary>
    private List<(EntityProp Console, List<int> InteractIds)> LiftCallConsoles(EntityProp lift)
    {
        var consoles = new List<(EntityProp, List<int>)>();
        foreach (var console in GetEntitiesInGroup<EntityProp>(lift.GroupId))
        {
            if (console == lift || console.IsGmSpawned || !console.PropInfo.GraphReleasesInteractOnOpen) continue;
            var interactIds = console.PropInfo.InteractOnOpenLinks
                .Where(link => link.GroupId == lift.GroupId && link.InstId == lift.InstId &&
                               GameData.InteractConfigData.TryGetValue(link.InteractId, out var row) &&
                               IsLiftPosition(row.SrcState) && IsLiftPosition(row.TargetState))
                .Select(link => link.InteractId)
                .ToList();
            if (interactIds.Count > 0) consoles.Add((console, interactIds));
        }

        return consoles;
    }

    private static bool IsLiftPosition(PropStateEnum state) =>
        state is >= PropStateEnum.Elevator1 and <= PropStateEnum.Elevator3;

    /// <summary>
    ///     A <c>BaseConsole_*_Control_SimpleDoor</c> console at Open must have its doors Open too. Its client graph
    ///     animates the door open when the console reaches Open and has no close path, so a save carrying the
    ///     console's Open without the door's rebuilt the gate shut with no way to reopen it (floor 20123001: console
    ///     8/300005 Open, gate 8/300004 Closed, blocking "Follow Perkins the robot"). Official 4.5 ships console and
    ///     gate Open together and keeps both. Only a Closed door whose state list declares Open moves.
    /// </summary>
    public async ValueTask OpenSimpleDoorGates()
    {
        foreach (var console in Entities.Values.OfType<EntityProp>().ToList())
        {
            if (console.State != PropStateEnum.Open ||
                console.PropInfo.InitLevelGraph?.Contains("Control_SimpleDoor", StringComparison.Ordinal) != true)
                continue;

            foreach (var (groupId, instIds) in console.PropInfo.UnlockDoorID)
            foreach (var instId in instIds)
            {
                var door = FindProp(groupId, instId);
                if (door is not { State: PropStateEnum.Closed } ||
                    !door.Excel.PropStateList.Contains(PropStateEnum.Open)) continue;
                await door.SetState(PropStateEnum.Open, false);
            }
        }
    }

    // Only a console still Locked moves, so a later step that already opened it is never reset.
    private async ValueTask UnlockLockedConsole(int groupId, int instId, bool sendPacket)
    {
        if (groupId == 0 || instId == 0) return;
        var console = FindProp(groupId, instId);
        if (console is not { State: PropStateEnum.Locked }) return;
        await console.SetState(PropStateEnum.Closed, sendPacket);
    }
}
