using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Scene;

/// <summary>
///     UnlockTeleportNotify. The 4.6 dump names DIJIDBDBJKB as entry_id; NCHMNBLCPCK is the remaining
///     teleport identifier. The original mapping is also proven by
///     the official wire values (4.4 part4.1.json idx 9655 fields 1/13 = 1001/2010101 vs 4.5 part5.1.json idx 7475
///     fields 3/10 = 1002/201110102 - the same Space Anchor MappingInfoID + floor MapEntrance pair, both sent right
///     after the SceneGroupRefreshScNotify that flipped the checkpoint to CheckPointEnable).
/// </summary>
public class PacketUnlockTeleportNotify : BasePacket
{
    public PacketUnlockTeleportNotify(int teleportId, int entryId) : base(CmdIds.UnlockTeleportNotify)
    {
        SetData(new UnlockTeleportNotify
        {
            NCHMNBLCPCK = (uint)teleportId,
            DIJIDBDBJKB = (uint)entryId
        });
    }
}
