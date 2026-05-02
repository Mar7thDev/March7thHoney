using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

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