using CastoriceHoney.Server.GameServer.Game.MultiPlayer;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Multiplayer;

public class PacketMultiplayerFightGameStartScNotify : BasePacket
{
    public PacketMultiplayerFightGameStartScNotify(BaseMultiPlayerGameRoomInstance room) : base(
        CmdIds.MultiplayerFightGameStartScNotify)
    {
        var proto = new MultiplayerFightGameStartScNotify
        {
            SessionInfo = room.ToSessionInfo(),
            LobbyBasicInfo = { room.ParentLobby.Players.Select(x => x.ToProto()) }
        };

        SetData(proto);
    }
}