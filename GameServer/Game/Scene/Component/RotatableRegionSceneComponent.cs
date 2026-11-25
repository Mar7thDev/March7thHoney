namespace March7thHoney.GameServer.Game.Scene.Component;

/// <summary>
///     Dreamscape bubble-bridge (map rotation) charges of one scene visit. The client builds the bridge, moves the
///     player and rotates the room; it deploys a bridge only while it holds a charge, so the server keeps the pool and
///     spends one per deployed volume. Not persisted: a bubble bridge is a per-visit puzzle.
/// </summary>
/// <remarks>
///     Official 4.7 flow on floor 43301001: enter (energy notify, then 3/3) → deploy g4/300001 (2/3) → rotate →
///     deploy g22/300003 (1/3) → reset, repeated while the player walks the room.
/// </remarks>
public class RotatableRegionSceneComponent(SceneInstance scene) : BaseSceneComponent(scene)
{
    /// <summary>
    ///     The region file of floor 43301001 declares no chargers while the official answers 3/3, so the ceiling is not
    ///     derivable. Any non-zero pool arms the client's deploy; zero soft-locks the corridor.
    /// </summary>
    public const int MaxEnergy = 5;

    private readonly HashSet<(int GroupId, int InstId)> _deployed = [];

    public int Energy { get; private set; } = MaxEnergy;
    public bool Active { get; private set; }

    public override async ValueTask Initialize()
    {
        await ValueTask.CompletedTask;
    }

    /// <summary>The scene's component, added on first use for a floor that does not declare the level feature.</summary>
    public static RotatableRegionSceneComponent Of(SceneInstance scene)
    {
        if (scene.GetComponent<RotatableRegionSceneComponent>() is { } existing) return existing;
        var component = new RotatableRegionSceneComponent(scene);
        scene.Components.Add(component);
        return component;
    }

    public void Enter()
    {
        Active = true;
        Refill();
    }

    /// <summary>A reset or a charger collapses the deployed bridges and refills the pool.</summary>
    public void Refill()
    {
        Energy = MaxEnergy;
        _deployed.Clear();
    }

    public void Leave()
    {
        Active = false;
    }

    /// <summary>
    ///     Spends one charge on a volume. A retry for a volume already deployed this visit spends nothing, and a
    ///     deploy with no preceding enter (a relog inside the corridor) enters first instead of being refused.
    /// </summary>
    public bool Deploy(int groupId, int instId)
    {
        if (!Active) Enter();
        if (_deployed.Contains((groupId, instId))) return true;
        if (Energy == 0) return false;

        Energy--;
        _deployed.Add((groupId, instId));
        return true;
    }
}
