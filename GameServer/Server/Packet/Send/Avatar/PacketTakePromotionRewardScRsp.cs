using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Avatar;

public class PacketTakePromotionRewardScRsp : BasePacket
{
    public PacketTakePromotionRewardScRsp() : base(CmdIds.TakePromotionRewardScRsp)
    {
        var itemList = new Proto.Item
        {
            ItemId = 101,
            Num = 1
        };

        var rsp = new TakePromotionRewardScRsp
        {
            RewardList = new ItemList
            {
                ItemList_ = { itemList }
            }
        };

        SetData(rsp);
    }
}
