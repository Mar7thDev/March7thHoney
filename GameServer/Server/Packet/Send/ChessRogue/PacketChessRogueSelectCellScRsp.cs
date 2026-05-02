using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueSelectCellScRsp : BasePacket
{
    public PacketChessRogueSelectCellScRsp(int cellId) : base(CmdIds.ChessRogueSelectCellScRsp)
    {
        var proto = new ChessRogueSelectCellScRsp
        {
            CellId = (uint)cellId
        };

        SetData(proto);
    }
}