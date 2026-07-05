using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Lineup;

public class PacketVirtualLineupTrialAvatarChangeScNotify : BasePacket
{
    public PacketVirtualLineupTrialAvatarChangeScNotify(int planeId, bool isAdd, List<int> specialAvatarIds) : base(
        CmdIds.VirtualLineupTrialAvatarChangeScNotify)
    {
        var proto = new VirtualLineupTrialAvatarChangeScNotify
        {
            OLPNMKAPDLP = isAdd,
            PlaneId = (uint)planeId
        };
        var list = isAdd ? proto.EDBMIFNEHHH : proto.ILIDIIGALKD;
        foreach (var id in specialAvatarIds) list.Add((uint)id);

        SetData(proto);
    }
}
