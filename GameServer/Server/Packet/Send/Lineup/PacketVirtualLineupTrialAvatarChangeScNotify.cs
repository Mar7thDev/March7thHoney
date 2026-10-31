using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Lineup;

public class PacketVirtualLineupTrialAvatarChangeScNotify : BasePacket
{
    public PacketVirtualLineupTrialAvatarChangeScNotify(int planeId, IEnumerable<int> added, IEnumerable<int> removed,
        bool joined) : base(CmdIds.VirtualLineupTrialAvatarChangeScNotify)
    {
        var proto = new VirtualLineupTrialAvatarChangeScNotify
        {
            PlaneId = (uint)planeId,
            OLPNMKAPDLP = joined
        };
        foreach (var id in added)
            if (id != 0)
                proto.EDBMIFNEHHH.Add((uint)id);
        foreach (var id in removed)
            if (id != 0)
                proto.ILIDIIGALKD.Add((uint)id);

        SetData(proto);
    }
}
