using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueUpdateLevelBaseInfoScNotify : BasePacket
{
    public PacketChessRogueUpdateLevelBaseInfoScNotify(ChessRogueLevelStatus status) : base(
        CmdIds.ChessRogueUpdateLevelBaseInfoScNotify)
    {
        var proto = new ChessRogueUpdateLevelBaseInfoScNotify
        {
            LevelStatus = status
        };

        SetData(proto);
    }
}