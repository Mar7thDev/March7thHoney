using MemoryPack;
using March7thHoney.Enums.Scene;

namespace March7thHoney.Data.Config.Scene;

/// <summary>
///     One server-side <c>RPG.GameCore.PropReqInteract</c> edge declared by a prop's InitLevelGraph, with every
///     target already resolved to a concrete (group, inst) pair at resource-load time.
/// </summary>
/// <remarks>
///     The client runs the visible half of a prop graph (animations, prefabs, camera); the official server
///     performs the STATE half - the PropReqInteract requests the graph makes on sibling props. The clearest
///     case is the robot treasure hunt (floor 20111001 group 47): interacting with the scan trigger fans out
///     into <c>PropReqInteract(120120)</c> on the guarded chest, which flips it Hidden → ChestClosed so a real
///     treasure appears. Without that fan-out the chest never materialises on a private server.
///     Key references (<c>SinglePropByPropKey</c>) are resolved through the graph OWNER's ValueSource
///     (e.g. <c>TargetChestClear_M301_01 → "47,300007"</c>) while the raw JSON is still in memory.
/// </remarks>
[MemoryPackable]
public partial class PropInteractLink
{
    public int GroupId { get; set; }
    public int InstId { get; set; }
    public int InteractId { get; set; }

    /// <summary>
    ///     The enclosing <c>PropStateExecute{State}</c> chain: the edge only fires while every guard prop is
    ///     in the recorded state, evaluated against the LIVE scene at fan-out time (including states the same
    ///     fan-out pass produced - the robot chest reveal is guarded by the state its first wave sets).
    /// </summary>
    public List<PropInteractGuard> Guards { get; set; } = [];
}

[MemoryPackable]
public partial class PropInteractGuard
{
    public int GroupId { get; set; }
    public int InstId { get; set; }
    public PropStateEnum State { get; set; }
}
