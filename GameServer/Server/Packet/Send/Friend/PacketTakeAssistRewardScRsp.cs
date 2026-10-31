using March7thHoney.Database.Inventory;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Friend;

/// <summary>
///     The 4.7 protocol has no message named TakeAssistRewardScRsp. ENEHBCOCEGB (cmd 2949) is the structural match
///     (retcode, per-borrower rows, reward); unverified until a 4.7 capture confirms it.
/// </summary>
public class PacketTakeAssistRewardScRsp : BasePacket
{
    public PacketTakeAssistRewardScRsp(IEnumerable<KAODLPJMEFE> rows, IEnumerable<ItemData> items)
        : base(CmdIds.ENEHBCOCEGB)
    {
        var proto = new ENEHBCOCEGB { Reward = new ItemList() };
        proto.OILHLECGBCH.AddRange(rows);
        foreach (var item in items) proto.Reward.ItemList_.Add(item.ToProto());

        SetData(proto);
    }
}
