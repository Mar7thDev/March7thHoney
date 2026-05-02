using March7thHoney.GameServer.Game.MultiPlayer;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Fight;

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