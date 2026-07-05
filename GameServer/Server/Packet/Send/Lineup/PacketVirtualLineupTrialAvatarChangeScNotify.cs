using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Lineup;

public class PacketVirtualLineupTrialAvatarChangeScNotify : BasePacket
{
    public PacketVirtualLineupTrialAvatarChangeScNotify(int planeId, List<int> addSpecialAvatarIds,
        List<int>? removeSpecialAvatarIds = null) : base(CmdIds.VirtualLineupTrialAvatarChangeScNotify)
    {
        var proto = new VirtualLineupTrialAvatarChangeScNotify
        {
            OLPNMKAPDLP = true,
            PlaneId = (uint)planeId
        };
        foreach (var id in addSpecialAvatarIds) proto.EDBMIFNEHHH.Add((uint)id);
        if (removeSpecialAvatarIds != null)
            foreach (var id in removeSpecialAvatarIds)
                proto.ILIDIIGALKD.Add((uint)id);

        SetData(proto);
    }
}
