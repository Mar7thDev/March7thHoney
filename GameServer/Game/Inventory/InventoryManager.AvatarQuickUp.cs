using March7thHoney.Data;
using March7thHoney.Database.Inventory;
using March7thHoney.Enums.Item;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Sync;
using March7thHoney.GameServer.Server.Packet.Send.PlayerSync;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Inventory;

public partial class InventoryManager
{
    private readonly SemaphoreSlim _avatarQuickUpLock = new(1, 1);

    public async ValueTask<(Retcode Retcode, List<ItemData> ReturnItems)> QuickLevelUpAvatar(EKLIDIDKFHC req)
    {
        await _avatarQuickUpLock.WaitAsync();
        try
        {
            return await ApplyAvatarQuickUp(req);
        }
        finally
        {
            _avatarQuickUpLock.Release();
        }
    }

    private async ValueTask<(Retcode, List<ItemData>)> ApplyAvatarQuickUp(EKLIDIDKFHC req)
    {
        var avatar = Player.AvatarManager!.GetFormalAvatar((int)req.BaseAvatarId);
        if (avatar == null) return (Retcode.RetAvatarNotExist, []);
        if (!GameData.AvatarConfigData.TryGetValue(avatar.AvatarId, out var config))
            return (Retcode.RetItemConfigNotExist, []);

        // Capture: IGOOJKMPMDC=30 and KLMIBJIKIBB=1 produce level 30 / promotion 1.
        // BACAOLDADMC holds EXP books; OAINNDAHIMG holds the separate promotion costs.
        var level = req.IGOOJKMPMDC;
        var promotion = req.KLMIBJIKIBB;
        if (level is < 1 or > 80 || level < avatar.Level || promotion < avatar.Promotion || promotion > config.MaxPromotion)
            return (Retcode.RetReqParaInvalid, []);
        if (level == avatar.Level && promotion == avatar.Promotion)
            return (Retcode.RetAvatarExpReachPromotionLimit, []); // Includes replayed successful requests.
        if (!GameData.AvatarPromotionConfigData.TryGetValue(avatar.AvatarId * 10 + (int)promotion, out var targetConfig))
            return (Retcode.RetItemConfigNotExist, []);
        if (level > targetConfig.MaxLevel) return (Retcode.RetAvatarExpReachPromotionLimit, []);

        var promotionCosts = new Dictionary<int, long>();
        for (var p = avatar.Promotion; p < promotion; p++)
        {
            if (!GameData.AvatarPromotionConfigData.TryGetValue(avatar.AvatarId * 10 + p, out var step))
                return (Retcode.RetItemConfigNotExist, []);
            if (level < step.MaxLevel || Player.Data.Level < step.PlayerLevelRequire || Player.Data.WorldLevel < step.WorldLevelRequire)
                return (Retcode.RetAvatarPromotionNotMeet, []);
            foreach (var cost in step.PromotionCostList)
                promotionCosts[cost.ItemID] = promotionCosts.GetValueOrDefault(cost.ItemID) + cost.ItemNum;
        }
        if (!ReadQuickUpCosts(req.OAINNDAHIMG, out var submittedPromotion) ||
            submittedPromotion.Count != promotionCosts.Count ||
            promotionCosts.Any(cost => submittedPromotion.GetValueOrDefault(cost.Key) != cost.Value))
            return (Retcode.RetItemNotInCostList, []);
        if (!ReadQuickUpCosts(req.BACAOLDADMC, out var costs)) return (Retcode.RetReqParaInvalid, []);

        long providedExp = 0;
        foreach (var (itemId, count) in costs)
        {
            if (!GameData.AvatarExpItemConfigData.TryGetValue(itemId, out var book) || book.Exp <= 0 ||
                !GameData.ItemConfigData.TryGetValue(itemId, out var item) || item.ItemMainType != ItemMainTypeEnum.Material)
                return (Retcode.RetAvatarExpItemNotExist, []);
            providedExp += book.Exp * count;
            if (providedExp > int.MaxValue) return (Retcode.RetReqParaInvalid, []);
        }
        long requiredExp = -avatar.Exp;
        for (var current = avatar.Level; current < level; current++)
        {
            var exp = GameData.GetAvatarExpRequired(config.ExpGroup, current);
            if (exp <= 0) return (Retcode.RetItemConfigNotExist, []);
            requiredExp += exp;
        }
        requiredExp = Math.Max(0, requiredExp);
        if (providedExp < requiredExp) return (Retcode.RetAvatarResExpNotEnough, []);
        foreach (var (itemId, count) in promotionCosts)
            costs[itemId] = costs.GetValueOrDefault(itemId) + count;
        costs[2] = costs.GetValueOrDefault(2) + providedExp / 10;
        foreach (var (itemId, count) in costs)
        {
            if (count > int.MaxValue || !GameData.ItemConfigData.ContainsKey(itemId))
                return (Retcode.RetReqParaInvalid, []);
            if ((GetItem(itemId)?.Count ?? 0) < count)
                return (itemId == 2 ? Retcode.RetScoinNotEnough : Retcode.RetItemNotEnough, []);
        }

        // Validate every cost before consuming anything. Publish the final avatar, currency and
        // material balances together before the quick-up response, matching the captured order.
        var changedItems = new Dictionary<int, ItemData>();
        foreach (var (itemId, count) in costs.Where(cost => cost.Value > 0))
        {
            var removed = await RemoveItem(itemId, (int)count, sync: false);
            if (itemId != 2 && removed != null) changedItems[itemId] = removed;
        }
        var returns = new List<ItemData>();
        var leftover = providedExp - requiredExp;
        foreach (var book in GameData.AvatarExpItemConfigData.Values.Where(book => book.Exp > 0)
                     .OrderByDescending(book => book.Exp).ThenBy(book => book.ItemID))
        {
            if (!GameData.ItemConfigData.TryGetValue(book.ItemID, out var item) || item.ItemMainType != ItemMainTypeEnum.Material) continue;
            var count = (int)(leftover / book.Exp);
            if (count == 0) continue;
            changedItems[book.ItemID] = await PutItem(book.ItemID, count);
            returns.Add(new ItemData { ItemId = book.ItemID, Count = count });
            leftover %= book.Exp;
        }
        var leveled = level > avatar.Level;
        avatar.Level = (int)level;
        avatar.Promotion = (int)promotion;
        // A promotion-only quick up retains existing EXP; level-targeted picks refund surplus books.
        if (leveled) avatar.Exp = 0;
        MarkDirty();
        var sync = new List<BaseSyncData> { new AvatarSyncData(avatar), new BasicInfoSyncData(Player.ToProto()) };
        sync.AddRange(changedItems.Values.Select(item => (BaseSyncData)new ItemSyncData(item)));
        await Player.SendPacket(new PacketPlayerSyncScNotify(sync));
        if (leveled) await Player.MissionManager!.TriggerCountEvent(MissionFinishTypeEnum.AnyAvatarLevelUp);
        return (Retcode.RetSucc, returns);
    }

    private static bool ReadQuickUpCosts(ItemCostData? data, out Dictionary<int, long> costs)
    {
        costs = [];
        if (data == null) return true;
        foreach (var cost in data.ItemList)
        {
            if (cost.PileItem is not { ItemId: > 0, ItemNum: > 0 } pile || pile.ItemId > int.MaxValue || pile.ItemNum > int.MaxValue)
                return false;
            var id = (int)pile.ItemId;
            costs[id] = costs.GetValueOrDefault(id) + pile.ItemNum;
            if (costs[id] > int.MaxValue) return false;
        }
        return true;
    }
}
