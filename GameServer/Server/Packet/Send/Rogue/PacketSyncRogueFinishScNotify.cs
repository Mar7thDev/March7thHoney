using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketSyncRogueFinishScNotify : BasePacket
{
    public PacketSyncRogueFinishScNotify(GKHGFCBMLBL finishInfo) : base(CmdIds.SyncRogueFinishScNotify)
    {
        SetData(new SyncRogueFinishScNotify { NLEOMLNOKCC = finishInfo });
    }
}
