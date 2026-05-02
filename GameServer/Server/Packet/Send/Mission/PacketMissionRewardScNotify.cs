using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Mission;

public class PacketMissionRewardScNotify : BasePacket
{
    public PacketMissionRewardScNotify(int mainMissionId, int subMissionId, List<ItemData> item) : base(
        CmdIds.MissionRewardScNotify)
    {
        var proto = new MissionRewardScNotify
        {
            MainMissionId = (uint)mainMissionId,
            SubMissionId = (uint)subMissionId,
            Reward = new ItemList()
        };

        foreach (var i in item) proto.Reward.ItemList_.Add(i.ToProto());

        SetData(proto);
    }
}