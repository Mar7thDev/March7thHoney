using March7thHoney.GameServer.Game.ChessRogue;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueLeaveScRsp : BasePacket
{
    public PacketChessRogueLeaveScRsp(ChessRogueInstance instance) : base(CmdIds.ChessRogueLeaveScRsp)
    {
        var proto = new ChessRogueLeaveScRsp
        {
            StageInfo = instance.ToStageProto(),
            QueryInfo = instance.Player.ChessRogueManager!.ToQueryInfo(),
            RogueAeonInfo = instance.Player.ChessRogueManager!.ToRogueAeonInfo(),
            RogueGetInfo = instance.Player.ChessRogueManager!.ToGetInfo()
        };

        SetData(proto);
    }
}