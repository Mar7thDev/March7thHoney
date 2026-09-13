using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketSyncRogueExploreWinScNotify : BasePacket
{
    public PacketSyncRogueExploreWinScNotify() : base(CmdIds.SyncRogueExploreWinScNotify)
    {
        SetData(new SyncRogueExploreWinScNotify { ILOFGCAFHAH = true });
    }
}
