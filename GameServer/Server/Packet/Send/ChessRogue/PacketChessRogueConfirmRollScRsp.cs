using March7thHoney.GameServer.Game.ChessRogue.Dice;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueConfirmRollScRsp : BasePacket
{
    public PacketChessRogueConfirmRollScRsp(ChessRogueDiceInstance dice) : base(CmdIds.ChessRogueConfirmRollScRsp)
    {
        var proto = new ChessRogueConfirmRollScRsp
        {
            RogueDiceInfo = dice.ToProto()
        };

        SetData(proto);
    }
}