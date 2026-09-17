using March7thHoney.Data;
using March7thHoney.Enums.GridFight;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed class GridFightEquipmentTrackState
{
    public uint RoleId { get; init; }
    public List<uint> TargetEquipmentIds { get; } = [];
    public List<uint> CompletedEquipmentIds { get; } = [];
    public GridFightEquipTrackSource Source { get; set; }
    public uint Priority { get; set; }
}

public sealed partial class GridFightSession
{
    public const uint EquipmentTrackMaxRetcode = 6952;
    public const int MaxEquipmentTrackRoles = 3;

    private readonly List<GridFightEquipmentTrackState> _equipmentTracks = [];
    private readonly List<GridFightRoleState> _pendingTrackedAutoEquipRoles = [];

    public IReadOnlyList<GridFightEquipmentTrackState> EquipmentTracks => _equipmentTracks;

    public uint UpdateEquipmentTracks(IEnumerable<HLHHLIGPNOM> updates, GridFightEquipTrackSource requestSource)
    {
        var incoming = updates.ToList();
        var simulated = _equipmentTracks.Select(CloneEquipmentTrack).ToList();
        foreach (var update in incoming)
        {
            if (update.BGFFICLCKHE == 0 ||
                !GameData.GridFightRoleBasicInfoData.ContainsKey(update.BGFFICLCKHE) ||
                update.GridFightItemList.Any(id => !GameData.GridFightEquipmentData.ContainsKey(id)))
                return (uint)Retcode.RetFail;
            var existing = simulated.FirstOrDefault(track => track.RoleId == update.BGFFICLCKHE);
            if (!update.BJDOINAHDLN)
            {
                if (existing != null) simulated.Remove(existing);
                continue;
            }

            if (existing == null)
            {
                if (simulated.Count >= MaxEquipmentTrackRoles) return EquipmentTrackMaxRetcode;
                existing = new GridFightEquipmentTrackState { RoleId = update.BGFFICLCKHE };
                simulated.Add(existing);
            }
            existing.TargetEquipmentIds.Clear();
            existing.TargetEquipmentIds.AddRange(update.GridFightItemList.Distinct());
            existing.CompletedEquipmentIds.Clear();
            existing.Source = update.Source != GridFightEquipTrackSource.KgridFightEquipTrackSourceNone
                ? update.Source
                : requestSource;
            existing.Priority = update.Priority;
        }

        _equipmentTracks.Clear();
        _equipmentTracks.AddRange(simulated.OrderBy(track => track.Priority == 0 ? uint.MaxValue : track.Priority));
        foreach (var role in Roles) RefreshEquipmentTrackCompletionForRole(role);
        return 0;
    }

    private static GridFightEquipmentTrackState CloneEquipmentTrack(GridFightEquipmentTrackState source)
    {
        var clone = new GridFightEquipmentTrackState
        {
            RoleId = source.RoleId,
            Source = source.Source,
            Priority = source.Priority,
        };
        clone.TargetEquipmentIds.AddRange(source.TargetEquipmentIds);
        clone.CompletedEquipmentIds.AddRange(source.CompletedEquipmentIds);
        return clone;
    }


    private void TryAutoEquipCraftedEquipment(
        GridFightEquipmentState equipment,
        GridFightEquipCraftSource source,
        uint trackId)
    {
        var isTrackedCraft = source is
            GridFightEquipCraftSource.KgridFightEquipCraftSourceTrack or
            GridFightEquipCraftSource.KgridFightEquipCraftSourceRecommend;

        if (isTrackedCraft && trackId != 0)
        {
            // Official 4.5 uses GridFightEquipCraftCsReq.track_id as the exact target role UniqueId.
            // Do not fall back to RoleId/priority guessing when an explicit target was supplied.
            var role = Roles.FirstOrDefault(candidate => candidate.UniqueId == trackId);
            if (role == null || !CanAutoEquip(role, equipment))
                return;

            AutoEquipTrackedEquipment(role, equipment);
            return;
        }

        // Legacy compatibility for older clients/callers that omit TrackId. Manual crafts do not auto-dress.
        if (isTrackedCraft || source == GridFightEquipCraftSource.KgridFightEquipCraftSourceNone)
            TryAutoEquipTrackedEquipment(equipment);
    }

    private void TryAutoEquipTrackedEquipment(GridFightEquipmentState equipment)
    {
        foreach (var entry in _equipmentTracks) RefreshEquipmentTrackCompletion(entry);
        var tracks = _equipmentTracks
            .Where(entry => entry.TargetEquipmentIds.Contains(equipment.EquipmentId) &&
                            !entry.CompletedEquipmentIds.Contains(equipment.EquipmentId))
            .OrderBy(entry => entry.Priority == 0 ? uint.MaxValue : entry.Priority);
        foreach (var track in tracks)
        {
            var role = Roles
                .Where(candidate => candidate.RoleId == track.RoleId && CanAutoEquip(candidate, equipment))
                .OrderByDescending(candidate => candidate.Star)
                .ThenBy(candidate => candidate.UniqueId)
                .FirstOrDefault();
            if (role == null) continue;
            AutoEquipTrackedEquipment(role, equipment);
            return;
        }
    }

    private bool CanAutoEquip(GridFightRoleState role, GridFightEquipmentState equipment) =>
        !equipment.IsLocked && FindEquipmentWearer(equipment.UniqueId) == null &&
        CanDress(role, equipment) &&
        (GameData.GridFightEquipmentData[equipment.EquipmentId].EquipType == GridFightEquipmentTypeEnum.Implants ||
         GetEquippedEquipment(role).Count(item =>
             GameData.GridFightEquipmentData[item.EquipmentId].EquipType != GridFightEquipmentTypeEnum.Implants) <
         NormalEquipmentSlotCount);

    private void AutoEquipTrackedEquipment(GridFightRoleState role, GridFightEquipmentState equipment)
    {
        role.EquippedEquipmentUniqueIds.Add(equipment.UniqueId);
        ApplyDiceEquipmentToRole(role);
        RefreshEquipmentTrackCompletionForRole(role);
        if (_pendingTrackedAutoEquipRoles.All(candidate => candidate.UniqueId != role.UniqueId))
            _pendingTrackedAutoEquipRoles.Add(role);
    }

    private void RefreshEquipmentTrackCompletionForRole(GridFightRoleState role)
    {
        var track = _equipmentTracks.FirstOrDefault(entry => entry.RoleId == role.RoleId);
        if (track == null) return;
        RefreshEquipmentTrackCompletion(track);
    }

    private void RefreshEquipmentTrackCompletion(GridFightEquipmentTrackState track)
    {
        var equippedIds = Roles.Where(candidate => candidate.RoleId == track.RoleId)
            .SelectMany(candidate => candidate.EquippedEquipmentUniqueIds)
            .Select(uniqueId => Equipments.FirstOrDefault(item => item.UniqueId == uniqueId)?.EquipmentId ?? 0)
            .Where(id => id != 0)
            .ToHashSet();
        track.CompletedEquipmentIds.Clear();
        track.CompletedEquipmentIds.AddRange(track.TargetEquipmentIds.Where(equippedIds.Contains));
    }

    public IReadOnlyList<GridFightRoleState> TakePendingTrackedAutoEquipRoles()
    {
        if (_pendingTrackedAutoEquipRoles.Count == 0) return [];
        var result = _pendingTrackedAutoEquipRoles.ToList();
        _pendingTrackedAutoEquipRoles.Clear();
        return result;
    }
}

public sealed partial class GridFightManager
{
    public GridFightSyncUpdateResultScNotify BuildEquipmentTrackSync()
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (Session == null) return notify;
        var result = new GridFightSyncResultData();
        AppendEquipmentTrackState(result);
        notify.ResultList.Add(result);
        return notify;
    }

    private static RoleTrackEquipmentInfo BuildEquipmentTrackInfo(GridFightEquipmentTrackState track)
    {
        var info = new RoleTrackEquipmentInfo
        {
            BGFFICLCKHE = track.RoleId,
            Source = track.Source,
            Priority = track.Priority,
        };
        info.GridFightItemList.Add(track.TargetEquipmentIds);
        info.MCJMHBIECMN.Add(track.CompletedEquipmentIds);
        return info;
    }

    private void AppendEquipmentTrackState(GridFightSyncResultData result)
    {
        var session = Session;
        if (session == null) return;
        var payload = new MDKHMNKAECP();
        payload.MIIDIMOCPEB.Add(session.EquipmentTracks.Select(BuildEquipmentTrackInfo));
        result.UpdateDynamicList.Add(new GridFightSyncData { BMGFDIKDOBE = payload });
    }

    private void AppendPendingTrackedAutoEquipRoles(GridFightSyncResultData result)
    {
        if (Session == null) return;
        var roles = Session.TakePendingTrackedAutoEquipRoles();
        if (roles.Count == 0) return;
        foreach (var role in roles)
            result.UpdateDynamicList.Add(new GridFightSyncData { UpdateRoleInfo = BuildRole(role) });
        // Do not push the completed-track list here. Official 4.5 first sends the
        // automatic dress role update, then the client reports the refreshed
        // completed list through GridFightUpdateEquipTrackCsReq.
    }

    private void AppendPendingTrackedAutoEquipResult(
        GridFightSyncUpdateResultScNotify notify,
        GridFightUpdateSrcType source)
    {
        if (Session == null) return;
        var roles = Session.TakePendingTrackedAutoEquipRoles();
        if (roles.Count == 0) return;
        var result = new GridFightSyncResultData { UpdateSource = source };
        foreach (var role in roles)
            result.UpdateDynamicList.Add(new GridFightSyncData { UpdateRoleInfo = BuildRole(role) });
        notify.ResultList.Add(result);
        AppendDiceEquipmentRefreshSync(notify);
    }
}
