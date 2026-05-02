using CastoriceHoney.Server.GameServer.Game.ChessRogue.Dice;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

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