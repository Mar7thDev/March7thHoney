using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketSyncRogueCommonPendingActionScNotify : BasePacket
{
    public PacketSyncRogueCommonPendingActionScNotify(INHBMHCJJCN action) : base(
        CmdIds.SyncRogueCommonPendingActionScNotify)
    {
        SetData(new SyncRogueCommonPendingActionScNotify { Action = action, DLOJFEEHLIJ = 101 });
    }
}
