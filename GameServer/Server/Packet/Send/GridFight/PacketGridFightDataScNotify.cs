using CastoriceHoney.Server.GameServer.Game.GridFight.Sync;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightDataScNotify : BasePacket
{
    public PacketGridFightDataScNotify(GridFightSrc src, List<BaseGridFightSyncData> data) : base(CmdIds.GridFightDataScNotify)
    {
        var proto = new GridFightDataScNotify
        {
            GridUpdateSrc = src,
            UpdateDynamicList = { data.Select(x => x.ToProto()) }
        };

        SetData(proto);
    }

    public PacketGridFightDataScNotify(GridFightSrc src, params BaseGridFightSyncData[] data) : this(src, data.ToList())
    {
    }
}