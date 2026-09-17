using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed partial class GridFightManager
{
    private void AppendPepePortalInitialResults(GridFightSyncUpdateResultScNotify notify)
    {
        var session = Session;
        var npc = session?.PepeNpc;
        if (session == null || npc == null || !session.HasPepePortal) return;

        // Captured 4.5 order for portal 116:
        // 1) publish the pre-Pepe active-role value together with backstage capacity +1;
        // 2) add three locked emblems individually;
        // 3) create the NPC and bind the three equipment UIDs one by one;
        // 4) publish the display population count including Pepe.
        var capacityResult = NewPepePortalResult(session.SelectedPortalId, 101);
        capacityResult.UpdateDynamicList.Add(new GridFightSyncData { MaxActiveRoleCount = session.MaxActiveRoles });
        capacityResult.UpdateDynamicList.Add(new GridFightSyncData { JLOCMDIDPCI = session.OffFieldMaxCount });
        notify.ResultList.Add(capacityResult);

        var equipped = session.GetPepeEquippedEquipment();
        foreach (var equipment in equipped)
        {
            var add = NewPepePortalResult(session.SelectedPortalId, 102);
            var items = new GridFightGameItemSyncInfo();
            items.GridFightEquipmentList.Add(BuildEquipment(equipment));
            add.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = items });
            notify.ResultList.Add(add);
        }

        for (var index = 0; index < equipped.Count; index++)
        {
            var bind = NewPepePortalResult(session.SelectedPortalId, 102);
            if (index == 0)
                bind.UpdateDynamicList.Add(new GridFightSyncData { EJMDHDJCGDI = BuildPepeNpc(npc, 0) });
            bind.UpdateDynamicList.Add(new GridFightSyncData { DHLEOLKNAMI = BuildPepeNpc(npc, index + 1) });
            notify.ResultList.Add(bind);
        }

        var populationResult = NewPepePortalResult(session.SelectedPortalId, 102);
        populationResult.UpdateDynamicList.Add(new GridFightSyncData
        {
            MaxActiveRoleCount = session.PepeDisplayRoleCount,
        });
        notify.ResultList.Add(populationResult);
    }

    private void AppendPepeDropResult(GridFightSyncUpdateResultScNotify notify)
    {
        var session = Session;
        if (session == null) return;
        var drop = session.TakePendingPepeDrop();
        if (!drop.Success || drop.Equipment == null || drop.Npc == null) return;

        var result = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcPortalBuff,
        };
        result.EffectParamList.Add(new uint[] { GridFightSession.RoughStarPepePortalId, 102 });

        // Official drop keeps the same equipment UID in the inventory. It only clears the
        // locked flag and removes that UID from Pepe's worn-equipment list.
        var items = new GridFightGameItemSyncInfo();
        items.GridFightEquipmentList.Add(new GridFightEquipmentInfo
        {
            UniqueId = drop.Equipment.UniqueId,
            GridFightEquipmentId = drop.Equipment.EquipmentId,
        });
        result.UpdateDynamicList.Add(new GridFightSyncData { UpdateGameItemInfo = items });
        result.UpdateDynamicList.Add(new GridFightSyncData { DHLEOLKNAMI = BuildPepeNpc(drop.Npc) });
        notify.ResultList.Add(result);
    }

    private static GridFightSyncResultData NewPepePortalResult(uint portalId, uint effectId)
    {
        var result = new GridFightSyncResultData
        {
            UpdateSource = GridFightUpdateSrcType.KgridFightSrcPreInitialSupplySelect,
        };
        result.EffectParamList.Add(new uint[] { portalId, effectId });
        return result;
    }

    private static GridGameNpcInfo BuildPepeNpc(GridFightNpcState npc, int equipmentCount = -1)
    {
        var result = new GridGameNpcInfo
        {
            Id = npc.NpcId,
            UniqueId = npc.UniqueId,
            Pos = npc.Position,
        };
        var count = equipmentCount < 0
            ? npc.EquippedEquipmentUniqueIds.Count
            : Math.Min(equipmentCount, npc.EquippedEquipmentUniqueIds.Count);
        result.PNJIPHJJONG.Add(npc.EquippedEquipmentUniqueIds.Take(count));
        return result;
    }
}
