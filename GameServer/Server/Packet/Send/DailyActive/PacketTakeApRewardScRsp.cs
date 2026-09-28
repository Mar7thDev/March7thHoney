using March7thHoney.Database.Inventory;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.DailyActive;

public class PacketTakeApRewardScRsp : BasePacket
{
    public PacketTakeApRewardScRsp(Retcode retcode, uint level, List<ItemData> items) : base(CmdIds.TakeApRewardScRsp)
    {
        var proto = new TakeApRewardScRsp
        {
            Retcode = (uint)retcode,
            Level = level,
            Reward = new ItemList
            {
                ItemList_ = { items.Select(item => item.ToProto()) }
            }
        };
        SetData(proto);
    }
}
