using March7thHoney.GameServer.Game.ChessRogue;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueEnterNextLayerScRsp : BasePacket
{
    public PacketChessRogueEnterNextLayerScRsp(ChessRogueInstance rogue) : base(CmdIds.ChessRogueEnterNextLayerScRsp)
    {
        var proto = new ChessRogueEnterNextLayerScRsp
        {
            StageInfo = rogue.ToStageProto(),
            RogueGameInfo = rogue.ToRogueGameInfo()
        };

        SetData(proto);
    }
}