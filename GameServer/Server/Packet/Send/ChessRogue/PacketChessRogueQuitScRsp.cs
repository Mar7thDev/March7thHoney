using CastoriceHoney.Server.GameServer.Game.ChessRogue;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

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