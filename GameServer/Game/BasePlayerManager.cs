using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game;

public class BasePlayerManager(PlayerInstance player)
{
    public PlayerInstance Player { get; private set; } = player;
}