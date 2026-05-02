using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

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