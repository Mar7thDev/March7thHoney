namespace March7thHoney.GameServer.Game.Player.Components;

public class FiveDimComponent(PlayerInstance player) : BasePlayerComponent(player)
{
    public const uint DefaultMaxJumpEnergy = 5;

    public uint MaxJumpEnergy { get; set; } = DefaultMaxJumpEnergy;
}
