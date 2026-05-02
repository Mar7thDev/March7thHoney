using CastoriceHoney.Server.GameServer.Game.ChessRogue.Dice;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueUpdateDiceInfoScNotify : BasePacket
{
    public PacketChessRogueUpdateDiceInfoScNotify(ChessRogueDiceInstance dice) : base(
        CmdIds.ChessRogueUpdateDiceInfoScNotify)
    {
        var proto = new ChessRogueUpdateDiceInfoScNotify
        {
            RogueDiceInfo = dice.ToProto()
        };

        SetData(proto);
    }
}