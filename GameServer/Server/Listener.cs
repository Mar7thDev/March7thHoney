using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server;

public class Listener : March7thHoneyListener
{
    /// <summary>
    ///     Snapshot of every connection that carries a logged-in player, for status and command
    ///     lookups. Centralizes the <c>(con as Connection)?.Player</c> walk so callers filter on
    ///     players rather than re-casting raw connections.
    /// </summary>
    public static IEnumerable<(Connection Connection, PlayerInstance Player)> GetPlayerConnections()
    {
        foreach (var con in GetSnapshot())
            if (con is Connection { Player: { } player } gameConnection)
                yield return (gameConnection, player);
    }

    /// <summary>Connection of an online player by uid, in any session state past login.</summary>
    public static Connection? GetPlayerConnection(int uid)
    {
        return GetPlayerConnections().FirstOrDefault(t => t.Player.Uid == uid).Connection;
    }

    /// <summary>Connection of an online player by uid in <see cref="SessionStateEnum.ACTIVE" /> state.</summary>
    public static Connection? GetActiveConnection(int uid)
    {
        return GetPlayerConnections()
            .FirstOrDefault(t => t.Player.Uid == uid && t.Connection.State == SessionStateEnum.ACTIVE)
            .Connection;
    }
}
