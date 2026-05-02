using March7thHoney.GameServer.Game.GridFight.Sync;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightSyncUpdateResultScNotify : BasePacket
{
    public PacketGridFightSyncUpdateResultScNotify(List<BaseGridFightSyncData> data) : base(CmdIds.GridFightSyncUpdateResultScNotify)
    {
        var group = data.GroupBy(x => new { x.GroupId, x.Src });

        var proto = new GridFightSyncUpdateResultScNotify
        {
            SyncResultDataList =
            {
                group.Select(x => new GridFightSyncResultData
                {
                    GridUpdateSrc = x.Key.Src,
                    UpdateDynamicList = { x.Select(j => j.ToProto()) },
                    SyncEffectParamList = { x.SelectMany(j => j.SyncParams).ToHashSet() }
                })
            }
        };

        SetData(proto);
    }

    public PacketGridFightSyncUpdateResultScNotify( params BaseGridFightSyncData[] data) : this(data.ToList())
    {
    }
}