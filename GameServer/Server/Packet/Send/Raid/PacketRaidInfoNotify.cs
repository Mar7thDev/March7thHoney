using CastoriceHoney.Server.Database.Scene;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Raid;

public class PacketRaidInfoNotify : BasePacket
{
    public PacketRaidInfoNotify(RaidRecord record) : base(CmdIds.RaidInfoNotify)
    {
        var proto = new RaidInfoNotify
        {
            RaidId = (uint)record.RaidId,
            Status = record.Status,
            WorldLevel = (uint)record.WorldLevel,
            RaidFinishTime = (ulong)record.FinishTimeStamp,
            ItemList = new ItemList()
        };

        SetData(proto);
    }

    public PacketRaidInfoNotify() : base(CmdIds.RaidInfoNotify)
    {
        var proto = new RaidInfoNotify();

        SetData(proto);
    }
}