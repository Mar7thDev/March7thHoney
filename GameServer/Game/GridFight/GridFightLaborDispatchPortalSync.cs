using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed partial class GridFightManager
{
    private void AppendLaborDispatchPortalInitialResult(GridFightSyncUpdateResultScNotify notify)
    {
        var session = Session;
        var emblem = session?.LaborDispatchEmblem;
        if (session == null || emblem == null || !session.HasLaborDispatchPortal) return;

        // Official capture: source=PreInitialSupplySelect, effect=[118,101], equipment source=0.
        var result = NewLaborDispatchResult(GridFightUpdateSrcType.KgridFightSrcPreInitialSupplySelect);
        var items = new GridFightGameItemSyncInfo();
        items.GridFightEquipmentList.Add(BuildEquipment(emblem));
        result.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = items });
        notify.ResultList.Add(result);
    }

    private void AppendLaborDispatchBossReward(GridFightSyncUpdateResultScNotify notify)
    {
        var session = Session;
        var acquisition = session?.TakeLaborDispatchBossReward();
        if (session == null || acquisition is not { Success: true }) return;

        // Official boss-node capture emits the acquired role under PortalBuff, then normal merge
        // packets, followed by a PortalBuff update of the final merged role when applicable.
        var result = NewLaborDispatchResult(GridFightUpdateSrcType.KgridFightSrcPortalBuff);
        AppendAcquiredRoles(result, acquisition);
        notify.ResultList.Add(result);
        AppendRoleMergeResults(
            notify,
            GridFightUpdateSrcType.KgridFightSrcPortalBuff,
            [GridFightSession.LaborDispatchPortalId, GridFightSession.LaborDispatchEffectId],
            acquisition);
        AppendUnlockEffects(notify, acquisition);
    }

    private static GridFightSyncResultData NewLaborDispatchResult(GridFightUpdateSrcType source)
    {
        var result = new GridFightSyncResultData { UpdateSource = source };
        result.EffectParamList.Add(new uint[]
        {
            GridFightSession.LaborDispatchPortalId,
            GridFightSession.LaborDispatchEffectId,
        });
        return result;
    }
}
