using March7thHoney.Database.ChessRogue;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

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