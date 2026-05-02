using CastoriceHoney.Server.GameServer.Game.Lobby;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.MultiPlayer;

public abstract class BaseMultiPlayerGameRoomInstance(long roomId, LobbyRoomInstance parentLobby)
{
    public FightGameMode GameMode { get; } = parentLobby.GameMode;
    public long RoomId { get; } = roomId;
    public LobbyRoomInstance ParentLobby { get; } = parentLobby;
    public List<BaseGamePlayerInstance> Players { get; } = [];

    public BaseGamePlayerInstance? GetPlayerById(int uid)
    {
        return Players.FirstOrDefault(player => player.LobbyPlayer.Player.Uid == uid);
    }

    public FightSessionInfo ToSessionInfo()
    {
        return new FightSessionInfo
        {
            SessionGameMode = GameMode,
            SessionRoomId = (ulong)RoomId
        };
    }
}