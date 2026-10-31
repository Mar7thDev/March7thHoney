using March7thHoney.Database.Inventory;
using March7thHoney.Database.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Raid;

public class PacketRaidInfoNotify : BasePacket
{
    public PacketRaidInfoNotify(RaidRecord record, List<ItemData>? rewards = null) : base(CmdIds.RaidInfoNotify)
    {
        var proto = new RaidInfoNotify
        {
            RaidId = (uint)record.RaidId,
            Status = record.Status,
            WorldLevel = (uint)record.WorldLevel,
            RaidFinishTime = (ulong)record.FinishTimeStamp,
            ItemList = new ItemList()
        };
        foreach (var item in rewards ?? [])
            proto.ItemList.ItemList_.Add(item.ToProto());

        SetData(proto);
    }

    public PacketRaidInfoNotify() : base(CmdIds.RaidInfoNotify)
    {
        // The reset shape keeps an empty item_list like the record shape, so the client never sees it null.
        var proto = new RaidInfoNotify { ItemList = new ItemList() };

        SetData(proto);
    }
}
