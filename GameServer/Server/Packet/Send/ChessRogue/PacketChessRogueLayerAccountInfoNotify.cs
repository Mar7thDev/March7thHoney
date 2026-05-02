using March7thHoney.GameServer.Game.ChessRogue;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

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