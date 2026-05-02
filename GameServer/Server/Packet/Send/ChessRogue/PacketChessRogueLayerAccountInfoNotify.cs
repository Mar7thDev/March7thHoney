using CastoriceHoney.Server.GameServer.Game.ChessRogue;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueLayerAccountInfoNotify : BasePacket
{
    public PacketChessRogueLayerAccountInfoNotify(ChessRogueInstance rogue) : base(CmdIds
        .ChessRogueLayerAccountInfoNotify)
    {
        var proto = new ChessRogueLayerAccountInfoNotify
        {
            FinishInfo = rogue.ToFinishInfo()
            //LayerId = (uint)rogue.CurLayer,
        };

        SetData(proto);
    }
}