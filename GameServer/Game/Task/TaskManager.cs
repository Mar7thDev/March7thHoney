using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.GameServer.Game.Task.AvatarTask;

namespace CastoriceHoney.Server.GameServer.Game.Task;

public class TaskManager(PlayerInstance player) : BasePlayerManager(player)
{
    public PerformanceTrigger PerformanceTrigger { get; } = new(player);
    public LevelTask LevelTask { get; } = new(player);
    public SummonUnitLevelTask SummonUnitLevelTask { get; } = new();
    public AbilityLevelTask AbilityLevelTask { get; } = new(player);
    public MissionTaskTrigger MissionTaskTrigger { get; } = new(player);
    public SceneTaskTrigger SceneTaskTrigger { get; } = new(player);
}