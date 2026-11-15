using March7thHoney.Enums.GridFight;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed partial class GridFightSession
{
    public const uint GoldOwlbertAugmentId = 303201;
    public const uint EmblemOwlbertAugmentId = 303301;
    public const uint RecyclingOwlbertAugmentId = 332401;
    // No count is shipped in the server table. User-approved temporary amount, 2026-10-13.
    public const uint RecyclingOwlbertBagCount = 9;
    private long _goldOwlbertTickMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private uint _goldOwlbertPaidThisNode;
    private readonly List<GridFightNpcState> _owlbertNpcs = [];
    private readonly List<GridFightNpcState> _owlbertArrivals = [];
    public IReadOnlyList<GridFightNpcState> OwlbertNpcs => _owlbertNpcs;

    private void InitializeOwlbert(uint augmentId)
    {
        var npcId = augmentId switch
        {
            GoldOwlbertAugmentId => 901u,
            EmblemOwlbertAugmentId => 902u,
            RecyclingOwlbertAugmentId => 903u,
            _ => 0u,
        };
        if (npcId == 0 || _owlbertNpcs.Any(npc => npc.NpcId == npcId)) return;
        // Reserve the extra NPC cell before looking for a position on a full bench.
        var position = GetFreeBackPosition(LastUnlockedBackPosition(extraCells: 1));
        var npc = new GridFightNpcState
        {
            UniqueId = _nextEntityUniqueId++, NpcId = npcId, Position = position,
        };
        _owlbertNpcs.Add(npc);
        var equipmentId = augmentId switch
        {
            EmblemOwlbertAugmentId => WealthDiamondEquipmentId,
            RecyclingOwlbertAugmentId => 352201u,
            _ => 0u,
        };
        if (equipmentId != 0)
            for (var i = 0; i < 3; i++)
                if (TryAddEquipment(equipmentId, out var equipment, NpcEquipmentSource, isLocked: true))
                    npc.EquippedEquipmentUniqueIds.Add(equipment.UniqueId);
        _owlbertArrivals.Add(npc);

        var grant = augmentId switch
        {
            GoldOwlbertAugmentId => G,
            EmblemOwlbertAugmentId => G with
            {
                Items = [(_catalog.RollEquipment(GridFightEquipCategoryEnum.Emblem, 0), 9u)],
            },
            _ => G with { Items = [(352201, RecyclingOwlbertBagCount)] },
        };
        _augmentSelectionEffects.Add(ApplyAugmentGrant(augmentId, grant));
        if (augmentId == GoldOwlbertAugmentId) ResetGoldOwlbertClock();
    }

    private void ResetGoldOwlbertClock()
    {
        _goldOwlbertPaidThisNode = 0;
        _goldOwlbertTickMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    // Driven by the existing heartbeat/request loop. No detached timer or client-supplied clock.
    public bool AccrueGoldOwlbert(long nowMs)
    {
        if (!ActiveInvestmentIds.Contains(GoldOwlbertAugmentId) || _goldOwlbertPaidThisNode >= 200 ||
            nowMs <= _goldOwlbertTickMs) return false;
        var ticks = (ulong)(nowMs - _goldOwlbertTickMs) / 500;
        var amount = (uint)Math.Min(ticks, 200 - _goldOwlbertPaidThisNode);
        if (amount == 0) return false;
        _goldOwlbertTickMs += amount * 500L;
        _goldOwlbertPaidThisNode += amount;
        AddGold(amount);
        return true;
    }

    public IReadOnlyList<GridFightNpcState> TakeOwlbertArrivals() => Drain(_owlbertArrivals);
}

public sealed partial class GridFightManager
{
    public GridFightSyncUpdateResultScNotify? BuildOwlbertGoldTick()
    {
        if (Session == null || Session.Phase == GridFightSessionPhase.Defeated ||
            !Session.AccrueGoldOwlbert(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())) return null;
        var result = BuildGoldSync();
        Persist();
        return result;
    }

    private void AppendOwlbertArrivals(GridFightSyncUpdateResultScNotify notify)
    {
        if (Session == null) return;
        foreach (var npc in Session.TakeOwlbertArrivals())
        {
            var result = new GridFightSyncResultData { UpdateSource = GridFightUpdateSrcType.KgridFightSrcAugment };
            var items = new GridFightGameItemSyncInfo();
            items.GridFightEquipmentList.Add(Session.Equipments
                .Where(item => npc.EquippedEquipmentUniqueIds.Contains(item.UniqueId)).Select(BuildEquipment));
            if (items.GridFightEquipmentList.Count > 0)
                result.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = items });
            result.UpdateDynamicList.Add(new GridFightSyncData { JLOCMDIDPCI = Session.OffFieldMaxCount });
            result.UpdateDynamicList.Add(new GridFightSyncData { EJMDHDJCGDI = BuildPepeNpc(npc, 0) });
            if (npc.Position != 0)
                result.UpdateDynamicList.Add(new GridFightSyncData { DHLEOLKNAMI = BuildPepeNpc(npc) });
            notify.ResultList.Add(result);
        }
    }
}
