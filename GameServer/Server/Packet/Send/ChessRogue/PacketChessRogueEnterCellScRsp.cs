using CastoriceHoney.Server.GameServer.Game.ChessRogue;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueEnterCellScRsp : BasePacket
{
    public PacketChessRogueEnterCellScRsp(uint cellId, ChessRogueInstance rogue) : base(CmdIds.ChessRogueEnterCellScRsp)
    {
        var proto = new ChessRogueEnterCellScRsp
        {
            CellId = cellId,
            StageInfo = rogue.ToStageProto(),
            Info = rogue.ToRogueGameInfo()
        };

        SetData(proto);
    }
}