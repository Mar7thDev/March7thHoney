using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Mission;

public class PacketSyncTaskScRsp : BasePacket
{
    public PacketSyncTaskScRsp(string key) : base(CmdIds.MissionSyncTaskScRsp)
    {
        SetData(new SyncTaskScRsp
        {
            AFBDHEMJEPP = key
        });
    }
}
