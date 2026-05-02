using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;

public class PacketSyncRogueStatusScNotify : BasePacket
{
    public PacketSyncRogueStatusScNotify(RogueStatus status) : base(CmdIds.SyncRogueStatusScNotify)
    {
        var proto = new SyncRogueStatusScNotify
        {
            Status = status
        };

        SetData(proto);
    }
}