using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;

public class PacketSyncRogueExploreWinScNotify : BasePacket
{
    public PacketSyncRogueExploreWinScNotify() : base(CmdIds.SyncRogueExploreWinScNotify)
    {
        var proto = new SyncRogueExploreWinScNotify
        {
            IsExploreWin = true
        };

        SetData(proto);
    }
}