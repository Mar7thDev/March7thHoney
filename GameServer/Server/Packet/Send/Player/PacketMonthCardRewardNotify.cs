using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Player;

public class PacketMonthCardRewardNotify : BasePacket
{
    public PacketMonthCardRewardNotify(List<ItemData> items) : base(CmdIds.MonthCardRewardNotify)
    {
        var proto = new MonthCardRewardNotify
        {
            Reward = new ItemList
            {
                ItemList_ = { items.Select(x => x.ToProto()) }
            }
        };

        SetData(proto);
    }
}