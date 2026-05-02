using March7thHoney.GameServer.Game.ChessRogue.Dice;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueReRollDiceScRsp : BasePacket
{
    public PacketChessRogueReRollDiceScRsp(ChessRogueDiceInstance dice) : base(CmdIds.ChessRogueReRollDiceScRsp)
    {
        var proto = new ChessRogueReRollDiceScRsp
        {
            RogueDiceInfo = dice.ToProto()
        };

        SetData(proto);
    }
}