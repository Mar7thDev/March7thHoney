using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

/// <summary>
///     A run blessing applied to the run's buff-holder entity (scheme type 6, sequence 1). Official 4.7 sends it for
///     every draft pick and every enhance, at the blessing's new level.
/// </summary>
public class PacketSyncRogueBuffHolderChangeScNotify : BasePacket
{
    private const uint BuffHolderEntityId = 6 << 20 | 1;

    public PacketSyncRogueBuffHolderChangeScNotify(int buffId, int level) : base(
        CmdIds.SyncEntityBuffChangeListScNotify)
    {
        SetData(new SyncEntityBuffChangeListScNotify
        {
            EntityBuffChangeList =
            {
                new EntityBuffChangeInfo
                {
                    EntityId = BuffHolderEntityId,
                    BuffChangeInfo = new BuffInfo
                    {
                        BuffId = (uint)buffId,
                        Level = (uint)level,
                        Count = uint.MaxValue,
                        LifeTime = -1,
                        AddTimeMs = (ulong)Extensions.GetUnixMs()
                    }
                }
            }
        });
    }
}

/// <summary>The handbook entry a newly obtained blessing unlocks.</summary>
public class PacketSyncRogueHandbookBuffScNotify : BasePacket
{
    public PacketSyncRogueHandbookBuffScNotify(int buffId) : base(CmdIds.SyncRogueHandbookDataUpdateScNotify)
    {
        SetData(new SyncRogueHandbookDataUpdateScNotify
        {
            IPNAAGCMNLB = { new BPHIKPCNBHM { MazeBuffId = (uint)buffId } }
        });
    }
}
