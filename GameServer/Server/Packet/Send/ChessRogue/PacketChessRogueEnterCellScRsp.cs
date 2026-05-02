using March7thHoney.GameServer.Game.ChessRogue;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

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