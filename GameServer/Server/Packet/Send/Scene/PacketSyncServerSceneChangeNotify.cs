using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Scene;

public class PacketSyncServerSceneChangeNotify : BasePacket
{
    public PacketSyncServerSceneChangeNotify() : base(CmdIds.SyncServerSceneChangeNotify)
    {
        SetData(new SyncServerSceneChangeNotify());
    }
}
