using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed partial class GridFightManager
{
    private void AppendInvitationPortalInitialResult(GridFightSyncUpdateResultScNotify notify)
    {
        var session = Session;
        var equipment = session?.InvitationPortalEquipment;
        if (session == null || equipment == null || !session.HasInvitationPortal) return;

        // Captured official 4.5 portal 1118 result:
        // update_source = KGridFightSrcPreInitialSupplySelect
        // effect_param_list = [1118, 102]
        // add_game_item_info.grid_fight_equipment_list = [{ id=35052011, source=1, unique_id=... }]
        // Reuse the same protocol shape for all invitation portals; only portal ID / emblem ID differ.
        var result = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcPreInitialSupplySelect,
        };
        result.EffectParamList.Add(new uint[]
        {
            session.SelectedPortalId,
            GridFightSession.InvitationPortalEffectId,
        });

        var items = new GridFightGameItemSyncInfo();
        items.GridFightEquipmentList.Add(BuildEquipment(equipment));
        result.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = items });
        notify.ResultList.Add(result);
    }
}
