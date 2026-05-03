using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.GameServer.Game.Player;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightSyncUpdateResultScNotify : BasePacket
{
    public PacketGridFightSyncUpdateResultScNotify(PlayerInstance player, IEnumerable<GridFightPosInfo>? updatedPosList = null) : base(CmdIds.GridFightSyncUpdateResultScNotify)
    {
        SetData(player.GridFightManager!.BuildSyncUpdateNotify(updatedPosList));
    }
}
