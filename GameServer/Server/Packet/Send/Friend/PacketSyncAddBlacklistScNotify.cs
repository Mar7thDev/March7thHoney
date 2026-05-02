using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketSyncAddBlacklistScNotify : BasePacket
{
    public PacketSyncAddBlacklistScNotify(int uid)
        : base(CmdIds.SyncAddBlacklistScNotify)
    {
        var proto = new SyncAddBlacklistScNotify
        {
            Uid = (uint)uid
        };

        SetData(proto);
    }
}