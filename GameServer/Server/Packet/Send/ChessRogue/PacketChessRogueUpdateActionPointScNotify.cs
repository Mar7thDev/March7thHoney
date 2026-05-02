using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

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