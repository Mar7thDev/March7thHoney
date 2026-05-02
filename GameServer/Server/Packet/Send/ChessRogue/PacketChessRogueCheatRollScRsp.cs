using March7thHoney.GameServer.Game.ChessRogue.Dice;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueCheatRollScRsp : BasePacket
{
    public PacketChessRogueCheatRollScRsp(ChessRogueDiceInstance dice, int surfaceId) : base(
        CmdIds.ChessRogueCheatRollScRsp)
    {
        var proto = new ChessRogueCheatRollScRsp
        {
            RogueDiceInfo = dice.ToProto(),
            DiceSurfaceId = (uint)surfaceId
        };

        SetData(proto);
    }
}