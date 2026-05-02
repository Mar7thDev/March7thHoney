using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueQueryScRsp : BasePacket
{
    public PacketChessRogueQueryScRsp(PlayerInstance player) : base(CmdIds.ChessRogueQueryScRsp)
    {
        var proto = new ChessRogueQueryScRsp
        {
            RogueGetInfo = player.ChessRogueManager!.ToGetInfo(),
            Info = player.ChessRogueManager!.ToGameInfo(),
            QueryInfo = player.ChessRogueManager!.ToQueryInfo()
        };

        SetData(proto);
    }
}