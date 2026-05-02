using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

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