using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed partial class GridFightSession
{
    public const uint PrismaticProcurementAugmentId = 303101;
    public const uint GoldProcurementAugmentId = 201201;
    public const string ProcurementSavedValueKey = "Augment303101_SavedValue01";
    public const string GoldProcurementSavedValueKey = "Augment201201_SavedValue01";
    public uint ProcurementRefreshCount => _investmentNodeCounters.GetValueOrDefault(PrismaticProcurementAugmentId);
    public uint GoldProcurementRefreshCount => _investmentNodeCounters.GetValueOrDefault(GoldProcurementAugmentId);

    private IReadOnlyList<GridFightRoleBasicInfoExcel>? RollProcurementShop()
    {
        var prismaticDue = Advance(PrismaticProcurementAugmentId, 5);
        var goldDue = Advance(GoldProcurementAugmentId, 7);
        var marketDue = false;
        if (ActiveInvestmentIds.Contains(102201))
        {
            var count = _investmentNodeCounters.GetValueOrDefault(102201u) + 1;
            _investmentNodeCounters[102201] = count;
            marketDue = count == 1 || count % 4 == 0;
        }
        uint cost;
        if (prismaticDue || goldDue)
        {
            var leftmost = Roles.Where(role => IsBenchPosition(role.Position))
                .OrderBy(role => role.Position).FirstOrDefault();
            if (leftmost == null || !GameData.GridFightRoleBasicInfoData.TryGetValue(leftmost.RoleId, out var basic))
                return null;
            cost = basic.Rarity;
        }
        else if (marketDue) cost = 3;
        else return null;
        var excluded = GetMaxStarExcludedRoleIds();
        var silverWolf = _silverWolfCurrentRoleId == 0
            ? GridFightSpecialFeatures.SilverWolfThreeCostRoleId : _silverWolfCurrentRoleId;
        var candidates = _catalog.GetRolesByCost(cost, _temporarilyUnlockedRestrictedRoleIds)
            .Where(role => !excluded.Contains(role.ID) &&
                (!GridFightSpecialFeatures.IsSilverWolfLv999Role(role.ID) || role.ID == silverWolf)).ToList();
        if (candidates.Count == 0) return null;
        return Enumerable.Range(0, checked((int)_catalog.ShopSize))
            .Select(_ => candidates[_catalog.NextRandom(0, candidates.Count)]).ToList();

        bool Advance(uint id, uint interval)
        {
            if (!ActiveInvestmentIds.Contains(id)) return false;
            var count = (_investmentNodeCounters.GetValueOrDefault(id) + 1) % interval;
            _investmentNodeCounters[id] = count;
            return count == 0;
        }
    }
}

public sealed partial class GridFightManager
{
    private void AppendProcurementRefreshSync(GridFightSyncUpdateResultScNotify notify)
    {
        if (Session == null) return;
        foreach (var (id, key, count) in new[]
                 {
                     (GridFightSession.PrismaticProcurementAugmentId, GridFightSession.ProcurementSavedValueKey, Session.ProcurementRefreshCount),
                     (GridFightSession.GoldProcurementAugmentId, GridFightSession.GoldProcurementSavedValueKey, Session.GoldProcurementRefreshCount),
                 })
        {
            if (!Session.ActiveInvestmentIds.Contains(id)) continue;
            var saved = new GridFightSavedValueUpdate();
            saved.SavedValues[key] = count;
            var result = new GridFightSyncResultData { UpdateSource = GridFightUpdateSrcType.KgridFightSrcAugment };
            result.EffectParamList.Add(id);
            result.UpdateDynamicList.Add(new GridFightSyncData { SavedValueUpdate = saved });
            notify.ResultList.Add(result);
        }
    }
}
