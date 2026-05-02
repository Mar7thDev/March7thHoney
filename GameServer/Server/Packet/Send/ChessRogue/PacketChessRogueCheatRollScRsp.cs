using CastoriceHoney.Server.GameServer.Game.ChessRogue.Dice;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

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