using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueUpdateActionPointScNotify : BasePacket
{
    public PacketChessRogueUpdateActionPointScNotify(int actionPoint) : base(CmdIds.ChessRogueUpdateActionPointScNotify)
    {
        var proto = new ChessRogueUpdateActionPointScNotify
        {
            ActionPoint = actionPoint
        };

        SetData(proto);
    }
}