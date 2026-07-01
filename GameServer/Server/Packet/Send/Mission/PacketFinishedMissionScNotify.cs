using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Mission;

public class PacketFinishedMissionScNotify : BasePacket
{
    public PacketFinishedMissionScNotify(IEnumerable<int> mainMissionIds) : base(CmdIds.FinishedMissionScNotify)
    {
        var proto = new FinishedMissionScNotify();
        proto.FinishedMainMissionIdList.AddRange(mainMissionIds
            .Where(x => x > 0)
            .Select(x => (uint)x)
            .Distinct());
        SetData(proto);
    }

    public PacketFinishedMissionScNotify(int mainMissionId) : this([mainMissionId])
    {
    }
}
