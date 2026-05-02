using March7thHoney.GameServer.Game.MultiPlayer;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Multiplayer;

public class PacketMultiplayerFightGameFinishScNotify : BasePacket
{
    public PacketMultiplayerFightGameFinishScNotify(BaseMultiPlayerGameRoomInstance room) : base(
        CmdIds.MultiplayerFightGameFinishScNotify)
    {
        var proto = new MultiplayerFightGameFinishScNotify
        {
            SessionInfo = new FightSessionInfo
            {
                SessionGameMode = room.GameMode,
                SessionRoomId = (ulong)room.RoomId
            }
        };

        SetData(proto);
    }
}