using CastoriceHoney.Server.GameServer.Game.MultiPlayer;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Multiplayer;

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