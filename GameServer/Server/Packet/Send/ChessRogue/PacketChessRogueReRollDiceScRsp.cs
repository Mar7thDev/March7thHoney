using CastoriceHoney.Server.GameServer.Game.ChessRogue.Dice;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

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