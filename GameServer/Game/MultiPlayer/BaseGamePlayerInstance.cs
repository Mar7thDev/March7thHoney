using CastoriceHoney.Server.GameServer.Game.Lobby.Player;
using CastoriceHoney.Server.GameServer.Server;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Game.MultiPlayer;

public abstract class BaseGamePlayerInstance(LobbyPlayerInstance lobby)
{
    public LobbyPlayerInstance LobbyPlayer { get; } = lobby;
    public bool EnterGame { get; set; }
    public bool LeaveGame { get; set; }
    public Connection? Connection { get; set; }

    public async ValueTask SendPacket(BasePacket packet)
    {
        if (Connection == null) return;
        if (!Connection.IsOnline) return;

        await Connection.SendPacket(packet);
    }
}