using March7thHoney.Database.Inventory;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.DailyActive;

/// <summary>Every claimed tier and all of their rewards concatenated in tier order (official bulk-claim shape).</summary>
public class PacketTakeAllApRewardScRsp : BasePacket
{
    public PacketTakeAllApRewardScRsp(List<int> levels, List<ItemData> items) : base(CmdIds.TakeAllApRewardScRsp)
    {
        var proto = new TakeAllApRewardScRsp
        {
            Retcode = (uint)Retcode.RetSucc,
            Reward = new ItemList
            {
                ItemList_ = { items.Select(item => item.ToProto()) }
            }
        };
        proto.TakeRewardLevelList.AddRange(levels.Select(level => (uint)level));
        SetData(proto);
    }
}
