using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueStartScRsp : BasePacket
{
    public PacketChessRogueStartScRsp(PlayerInstance player) : base(CmdIds.ChessRogueStartScRsp)
    {
        var proto = new ChessRogueStartScRsp
        {
            StageInfo = player.ChessRogueManager!.RogueInstance!.ToStageProto(),
            Info = player.ChessRogueManager!.RogueInstance!.ToRogueGameInfo()
        };

        SetData(proto);
    }
}