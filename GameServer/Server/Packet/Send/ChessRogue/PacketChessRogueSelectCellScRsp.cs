using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

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