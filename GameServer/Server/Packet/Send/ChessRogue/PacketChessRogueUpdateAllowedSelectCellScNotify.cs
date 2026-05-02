using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

public class PacketChessRogueUpdateAllowedSelectCellScNotify : BasePacket
{
    public PacketChessRogueUpdateAllowedSelectCellScNotify(int boardId, List<int> allowed) : base(
        CmdIds.ChessRogueUpdateAllowedSelectCellScNotify)
    {
        var proto = new ChessRogueUpdateAllowedSelectCellScNotify
        {
            BoardId = (uint)boardId
        };

        foreach (var cell in allowed) proto.AllowSelectCellIdList.Add((uint)cell);

        SetData(proto);
    }
}