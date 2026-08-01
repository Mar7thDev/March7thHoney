using March7thHoney.Data;
using March7thHoney.Enums.GridFight;

namespace March7thHoney.GameServer.Game.GridFight;

public enum GridFightRoleUnlockEffect
{
    CyrenePoemShop,
    SilverWolfHackConsole,
}

public sealed record GridFightRewardEntry(
    GridFightBonusTypeEnum Type,
    uint ItemId,
    uint Count,
    uint RoleStar = 0);

public sealed class GridFightRewardApplication
{
    public bool Success { get; init; }
    public uint SourceUniqueId { get; init; }
    public uint SourceItemId { get; init; }
    public bool GoldChanged { get; set; }
    public bool PlayerLevelChanged { get; set; }
    public bool PlayerExpChanged { get; set; }
    public bool LineupHpChanged { get; set; }
    public bool RolePropertiesChanged { get; set; }
    public List<GridFightRewardEntry> Drops { get; } = [];
    public List<GridFightRoleAcquisitionResult> RoleAcquisitions { get; } = [];
    public List<GridFightEquipmentState> AddedEquipments { get; } = [];
    public List<GridFightConsumableChange> ConsumableChanges { get; } = [];
    public List<GridFightOrbState> AddedOrbs { get; } = [];
    public List<GridFightForgeItemState> AddedForgeItems { get; } = [];
}

public sealed record GridFightConsumableChange(
    GridFightConsumableState Consumable,
    bool IsNew,
    uint StackDelta);

public sealed class GridFightForgeItemState
{
    public uint UniqueId { get; init; }
    public uint ForgeItemId { get; init; }
    public uint Position { get; set; }
    public IReadOnlyList<uint> EquipmentChoices { get; init; } = [];
}

public sealed class GridFightHackConsoleState
{
    public uint GameItemUniqueId { get; init; }
    public uint RemainingGoldActions { get; set; } = GridFightSpecialFeatures.HackGoldActionCount;
    public uint RemainingHealActions { get; set; } = GridFightSpecialFeatures.HackHealActionCount;
    public uint RemainingOrbActions { get; set; } = GridFightSpecialFeatures.HackOrbActionCount;
    public uint EnemyHpPercent { get; set; } = GridFightSpecialFeatures.DefaultEnemyHpPercent;
}

public sealed record GridFightSpecialGoodsPurchaseResult(
    uint GoodsIndex,
    uint SpecialGoodsId,
    GridFightRewardApplication Application);

public sealed record GridFightTraitEnhanceActivationResult(
    bool Success,
    uint FinishedQueuePosition,
    GridFightEquipmentState? Equipment)
{
    public static GridFightTraitEnhanceActivationResult Failed { get; } = new(false, 0, null);
}

public sealed record GridFightHackOptionResult(
    bool Success,
    uint OptionId,
    GridFightRewardApplication Application)
{
    public static GridFightHackOptionResult Failed { get; } =
        new(false, 0, new GridFightRewardApplication());
}

public static class GridFightSpecialFeatures
{
    public const uint CyreneRoleId = 1415;
    public const uint SilverWolfFiveCostRoleId = 15063;
    public const uint CyreneHealGoodsId = 201;
    public const uint CyreneWealthGoodsId = 202;
    public const uint CyreneInsigniaGoodsId = 203;
    public const uint CyreneCombatGoodsId = 204;
    public const uint CyreneHeroGoodsId = 205;
    public const uint CyreneHealAmount = 100;
    public const uint CyreneWealthBonusId = 11059;
    public const uint CyreneInsigniaBonusId = 11060;
    public const uint CyreneHeroBonusId = 11063;
    public const uint SilverWolfTraitId = 3006;
    public const uint SilverWolfTraitEffectId = 30063;
    public const uint SilverWolfEnhanceOptionId = 25;
    public const uint SilverWolfHackEquipmentId = 352507;
    public const uint HackAddGoldOptionId = 1;
    public const uint HackFullHealOptionId = 2;
    public const uint HackGrantOrbOptionId = 3;
    public const uint HackGoldPerAction = 10;
    public const uint HackGoldActionCount = 99;
    public const uint HackHealActionCount = 1;
    public const uint HackOrbActionCount = 5;
    public const uint MinimumEnemyHpPercent = 1;
    public const uint MaximumEnemyHpPercent = 1000;
    public const uint DefaultEnemyHpPercent = MaximumEnemyHpPercent;
    public const uint HackEquipmentSource = 1;
    public const string HackRankFiveEquipmentConstName = "GridFight_Hack_Rank5";

    public static IReadOnlyList<uint> CyreneSpecialGoodsIds { get; } =
        [CyreneHealGoodsId, CyreneWealthGoodsId, CyreneInsigniaGoodsId, CyreneCombatGoodsId, CyreneHeroGoodsId];

    public static IReadOnlyList<uint> HackOrbIds { get; } = [111079, 111080, 111081, 111082, 111083];

    public static bool TryResolveSilverWolfEquipmentId(out uint equipmentId)
    {
        equipmentId = 0;
        if (!GameData.GridFightConstValueData.TryGetValue(HackRankFiveEquipmentConstName, out var rankFiveEquipment) ||
            !rankFiveEquipment.AsUInt32List().Contains(SilverWolfHackEquipmentId) ||
            !GameData.GridFightEquipmentData.TryGetValue(SilverWolfHackEquipmentId, out var equipment) ||
            equipment.EquipCategory != GridFightEquipCategoryEnum.Hack ||
            !equipment.DressRuleParamList.Contains(SilverWolfFiveCostRoleId))
            return false;

        equipmentId = equipment.ID;
        return true;
    }

    public static bool TryResolveOrb(uint orbItemId, out IReadOnlyList<GridFightRewardEntry> rewards)
    {
        rewards = [];
        if (!GameData.GridFightOrbData.TryGetValue(orbItemId, out var orb)) return false;
        return TryResolveBonus(orb.BonusID, out rewards);
    }

    public static bool TryResolveBonus(uint bonusId, out IReadOnlyList<GridFightRewardEntry> rewards)
    {
        var result = new List<GridFightRewardEntry>();
        var success = ResolveBonus(bonusId, 1, [], result);
        rewards = success ? result : [];
        return success;
    }

    private static bool ResolveBonus(
        uint bonusId,
        uint multiplier,
        HashSet<uint> resolving,
        List<GridFightRewardEntry> rewards)
    {
        if (!resolving.Add(bonusId)) return false;
        try
        {
            if (GameData.GridFightCombinationBonusData.TryGetValue(bonusId, out var combination))
            {
                if (combination.CombinationBonusList.Count != combination.BonusNumberList.Count) return false;
                for (var index = 0; index < combination.CombinationBonusList.Count; index++)
                {
                    var scaled = (ulong)multiplier * combination.BonusNumberList[index] / 10000;
                    if (scaled == 0 || scaled > uint.MaxValue ||
                        !ResolveBonus(combination.CombinationBonusList[index], (uint)scaled, resolving, rewards))
                        return false;
                }
                return true;
            }

            if (!GameData.GridFightBasicBonusPoolV2Data.TryGetValue(bonusId, out var basic)) return false;
            var itemId = basic.BonusTypeParamList.FirstOrDefault();
            if (itemId == 0) itemId = basic.BonusTypeParam;
            var roleStar = basic.BonusType == GridFightBonusTypeEnum.SpecificAvatar
                ? basic.BonusTypeParamList.ElementAtOrDefault(1)
                : 0;
            rewards.Add(new GridFightRewardEntry(basic.BonusType, itemId, multiplier, roleStar));
            return true;
        }
        finally
        {
            resolving.Remove(bonusId);
        }
    }
}
