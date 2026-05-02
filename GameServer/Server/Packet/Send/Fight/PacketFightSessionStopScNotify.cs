using CastoriceHoney.Server.GameServer.Game.MultiPlayer;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Fight;

public class PacketFightSessionStopScNotify : BasePacket
{
    public PacketFightSessionStopScNotify(BaseMultiPlayerGameRoomInstance room) : base(CmdIds.FightSessionStopScNotify)
    {
        var proto = new FightSessionStopScNotify
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