using March7thHoney.Database.Inventory;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity;

public class PacketTakeMaterialSubmitActivityRewardScRsp : BasePacket
{
    public PacketTakeMaterialSubmitActivityRewardScRsp(uint id, Retcode retcode, List<ItemData> items)
        : base(CmdIds.TakeMaterialSubmitActivityRewardScRsp)
    {
        var proto = new TakeMaterialSubmitActivityRewardScRsp
        {
            LNOCOOCMNCI = id,
            Retcode = (uint)retcode,
            Reward = new ItemList()
        };
        foreach (var item in items)
            proto.Reward.ItemList_.Add(item.ToProto());
        SetData(proto);
    }
}
