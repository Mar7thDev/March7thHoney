using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Mission;

public class PacketMissionAcceptScNotify : BasePacket
{
    public PacketMissionAcceptScNotify(int missionId) : this([missionId])
    {
    }

    public PacketMissionAcceptScNotify(List<int> missionIds) : base(1251)
    {
        var proto = new MainMissionAcceptNotify();
        foreach (var missionId in missionIds) proto.SubMissionIdList.Add((uint)missionId);

        SetData(proto);
    }
}

