using March7thHoney.GameServer.Game.ChessRogue;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueQuitScRsp : BasePacket
{
    public PacketChessRogueQuitScRsp(ChessRogueInstance instance) : base(CmdIds.ChessRogueQuitScRsp)
    {
        var proto = new ChessRogueQuitScRsp
        {
            FinishInfo = instance.ToFinishInfo(),
            LevelInfo = instance.ToLevelInfo(),
            StageInfo = instance.ToStageProto(),
            QueryInfo = instance.Player.ChessRogueManager!.ToQueryInfo(),
            RogueGetInfo = instance.Player.ChessRogueManager!.ToGetInfo(),
            RogueAeonInfo = instance.Player.ChessRogueManager!.ToRogueAeonInfo()
        };

        SetData(proto);
    }
}