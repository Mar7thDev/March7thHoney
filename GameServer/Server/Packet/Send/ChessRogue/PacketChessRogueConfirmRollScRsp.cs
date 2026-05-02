using CastoriceHoney.Server.GameServer.Game.ChessRogue.Dice;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

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