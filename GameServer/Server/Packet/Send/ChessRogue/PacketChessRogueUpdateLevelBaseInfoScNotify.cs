using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

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