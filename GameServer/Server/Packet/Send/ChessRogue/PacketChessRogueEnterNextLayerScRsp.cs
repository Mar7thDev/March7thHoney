using CastoriceHoney.Server.GameServer.Game.ChessRogue;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

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