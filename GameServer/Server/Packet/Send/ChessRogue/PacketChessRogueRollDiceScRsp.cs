using March7thHoney.GameServer.Game.ChessRogue.Dice;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueRollDiceScRsp : BasePacket
{
    public PacketChessRogueRollDiceScRsp(ChessRogueDiceInstance dice) : base(CmdIds.ChessRogueRollDiceScRsp)
    {
        var proto = new ChessRogueRollDiceScRsp
        {
            RogueDiceInfo = dice.ToProto()
        };

        SetData(proto);
    }
}