using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Scene;

/// <summary>
///     UnlockTeleportNotify (cmd 1492). The 4.5v3 dump only carries the obfuscated shape
///     <c>HMBCOANODNA { FLMPPLGOIPA = 3 (teleport_id), ELMGEBCLLFP = 10 (entry_id) }</c>; the mapping is proven by
///     the official wire values (4.4 part4.1.json idx 9655 fields 1/13 = 1001/2010101 vs 4.5 part5.1.json idx 7475
///     fields 3/10 = 1002/201110102 - the same Space Anchor MappingInfoID + floor MapEntrance pair, both sent right
///     after the SceneGroupRefreshScNotify that flipped the checkpoint to CheckPointEnable).
/// </summary>
public class PacketUnlockTeleportNotify : BasePacket
{
    public PacketUnlockTeleportNotify(int teleportId, int entryId) : base(CmdIds.HMBCOANODNA)
    {
        SetData(new HMBCOANODNA
        {
            FLMPPLGOIPA = (uint)teleportId,
            ELMGEBCLLFP = (uint)entryId
        });
    }
}
