using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Activity;

public class PacketTakeTrialActivityRewardScRsp : BasePacket
{
    public PacketTakeTrialActivityRewardScRsp(uint stageId, List<ItemData> rewards) : base(
        CmdIds.TakeTrialActivityRewardScRsp)
    {
        var proto = new TakeTrialActivityRewardScRsp
        {
            StageId = stageId,
            Reward = new ItemList()
        };
        proto.Reward.ItemList_.Add(rewards.Select(x => x.ToProto()).ToArray());

        SetData(proto);
    }
}