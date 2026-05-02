using March7thHoney.GameServer.Game.ChessRogue.Dice;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

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