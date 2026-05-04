using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightSyncUpdateResultScNotify : BasePacket
{
    public PacketGridFightSyncUpdateResultScNotify(PlayerInstance player, IEnumerable<GridFightPosInfo>? updatedPosList = null) : base(CmdIds.GridFightSyncUpdateResultScNotify)
    {
        SetData(player.GridFightManager!.BuildSyncUpdateNotify(updatedPosList));
    }

    public PacketGridFightSyncUpdateResultScNotify(PlayerInstance player, GridFightSyncKind kind, object? extra = null) : base(CmdIds.GridFightSyncUpdateResultScNotify)
    {
        SetData(player.GridFightManager!.BuildSyncUpdateNotify(null, kind, extra));
    }

    public PacketGridFightSyncUpdateResultScNotify(GridFightSyncUpdateResultScNotify built) : base(CmdIds.GridFightSyncUpdateResultScNotify)
    {
        SetData(built);
    }
}
