using CastoriceHoney.Server.Database.ChessRogue;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueNousEditDiceScRsp : BasePacket
{
    public PacketChessRogueNousEditDiceScRsp(ChessRogueNousDiceData diceData) : base(CmdIds.ChessRogueNousEditDiceScRsp)
    {
        var proto = new ChessRogueNousEditDiceScRsp
        {
            QueryDiceInfo = diceData.ToProto()
        };

        SetData(proto);
    }
}