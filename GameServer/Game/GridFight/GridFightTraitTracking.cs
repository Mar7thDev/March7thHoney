using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed partial class GridFightSession
{
    public const uint TraitTrackMaxRetcode = 6936;
    public const int MaxTraitTrackCount = 3;

    private readonly List<uint> _trackedTraitIds = [];
    public IReadOnlyList<uint> TrackedTraitIds => _trackedTraitIds;

    public uint UpdateTraitTrack(uint traitId, bool enabled)
    {
        // The client only exposes valid traits, and some compatibility/custom traits
        // do not necessarily exist in the bundled basic-info cache. Reject only zero
        // so those traits can still be tracked exactly like official runtime traits.
        if (traitId == 0) return (uint)Retcode.RetFail;

        if (!enabled)
        {
            _trackedTraitIds.Remove(traitId);
            return 0;
        }

        if (_trackedTraitIds.Contains(traitId)) return 0;
        if (_trackedTraitIds.Count >= MaxTraitTrackCount) return TraitTrackMaxRetcode;
        _trackedTraitIds.Add(traitId);
        return 0;
    }
}

public sealed partial class GridFightManager
{
    public GridFightSyncUpdateResultScNotify BuildTraitTrackSync()
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        var session = Session;
        if (session == null) return notify;

        var track = new GridFightTraitTrackSyncInfo();
        track.ONHDKLIMLNM.Add(session.TrackedTraitIds);
        var result = new GridFightSyncResultData();
        result.UpdateDynamicList.Add(new GridFightSyncData { HGNDMOODNKF = track });
        notify.ResultList.Add(result);
        return notify;
    }
}
