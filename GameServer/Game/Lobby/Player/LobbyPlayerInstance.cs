using March7thHoney.GameServer.Game.Player;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Lobby.Player;

public class LobbyPlayerInstance(PlayerInstance player, LobbyCharacterType characterType, LobbyRoomInstance lobby)
{
    public PlayerInstance Player { get; } = player;
    public List<int> EquippedSealList { get; set; } = [];
    public LobbyCharacterType CharacterType { get; set; } = characterType;
    public LobbyCharacterStatus CharacterStatus { get; set; } = LobbyCharacterStatus.Idle;
    public LobbyRoomInstance LobbyRoom { get; set; } = lobby;

    public LobbyBasicInfo ToProto()
    {
        return new LobbyBasicInfo
        {
            PlayerLobbyInfo = new LobbyPlayerInfo
            {
                CharacterType = CharacterType,
                Status = CharacterStatus
            },
            StageInfo = new LobbyGameInfo
            {
                LobbyMarbleInfo = new LobbyMarbleInfo
                {
                    LobbySealList = { EquippedSealList.Select(x => (uint)x) },
                    Rank = 1
                }
            },
            BasicInfo = Player.Data.ToLobbyProto()
        };
    }
}