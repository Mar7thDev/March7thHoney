using March7thHoney.Data;

namespace March7thHoney.GameServer.Game.GridFight;

/// <summary>
/// Shared implementation for all "概念股" opening portals.
/// Official packets show that the two featured roles occupy two of the ordinary four
/// initial-supply slots (they are not extra pre-supply roles), followed by two random roles.
/// These portals can override the ordinary opening total-cost=6 constraint.
/// The portal also grants one simple equipment and boosts same-rarity shop weight for roles
/// carrying the corresponding trait. Current capture sampling is modelled as a 1.5x weight.
/// </summary>
public sealed record GridFightConceptStockConfig(
    uint PortalId,
    uint TraitId,
    uint FirstRoleId,
    uint SecondRoleId,
    uint InitialEquipmentId);

public sealed partial class GridFightSession
{
    public const double ConceptStockShopWeightMultiplier = 1.5;

    private static readonly IReadOnlyDictionary<uint, GridFightConceptStockConfig> ConceptStockConfigs =
        new Dictionary<uint, GridFightConceptStockConfig>
        {
            [1001] = new(1001, 2001, 1220, 1305, 350201), // 追击: 飞霄 / 真理医生 / 轮滑鞋
            [1002] = new(1002, 2006, 1409, 1507, 350205), // 燃血: 风堇 / 千冶·刃 / 生命之花
            [1003] = new(1003, 2004, 1402, 1217, 350208), // 能量: 阿格莱雅 / 藿藿 / 光能电池
            [1004] = new(1004, 1001, 1001, 1510, 350204), // 列车同行: 三月七 / 姬子·启行 / 幸运星
            [1005] = new(1005, 1003, 1009, 1303, 350208), // 银河学者: 艾丝妲 / 阮·梅 / 光能电池
            [1006] = new(1006, 1005, 1105, 1102, 350206), // 贝洛伯格: 娜塔莎 / 希儿 / 量产型装甲
            [1007] = new(1007, 1006, 1302, 1305, 350207), // 星间旅人: 银枝 / 真理医生 / 以太钻头
            [1008] = new(1008, 1007, 1217, 1213, 350202), // 仙舟: 藿藿 / 丹恒·饮月 / 折叠小刀
            [1009] = new(1009, 1008, 1218, 1222, 350205), // 狼狩: 椒丘 / 灵砂 / 生命之花
            [1010] = new(1010, 1011, 1402, 1403, 350201), // 昼之半神: 阿格莱雅 / 缇宝 / 轮滑鞋
            [1011] = new(1011, 1012, 1406, 1412, 350203), // 夜之半神: 赛飞儿 / 刻律德菈 / 和平手枪
            [1012] = new(1012, 2002, 1225, 1317, 350207), // 击破: 忘归人 / 乱破 / 以太钻头
            [1013] = new(1013, 2003, 1013, 1405, 350204), // 群攻: 赫塔 / 那刻夏 / 幸运星
            [1014] = new(1014, 2007, 1507, 1308, 350203), // 减益: 千冶·刃 / 黄泉 / 和平手枪
            [1016] = new(1016, 2011, 1213, 1306, 350202), // 战技点: 丹恒·饮月 / 花火 / 折叠小刀(packet)
            // Season 2 (4.6.51): trait = GridFightPortalBuff.EffectParamList[1]; featured avatars and equipment
            // come from GridFightCombinationBonus[EffectParamList[0]] (season 1's 40001 gives the same 1220/1305/350201
            // as entry 1001). Avatar ids are resolved to the running season's role ids.
            [2201] = new(2201, 100102, 1221, 1222, 350202), // 仙舟舰队概念股 (bonus 42001)
            [2202] = new(2202, 100202, 1405, 1404, 350201), // 奇兽花园概念股 (bonus 42002)
            [2203] = new(2203, 100302, 1505, 1503, 350202), // 幻月游戏概念股 (bonus 42003)
            [2204] = new(2204, 100502, 1004, 1313, 350208), // 开拓银轨概念股 (bonus 42004)
            [2205] = new(2205, 100602, 1112, 1503, 350201), // 千星商会概念股 (bonus 42005)
            [2206] = new(2206, 200102, 1220, 1112, 350201), // 追击概念股 (bonus 42006)
            [2207] = new(2207, 200202, 1210, 1307, 350203), // 持续伤害概念股 (bonus 42007)
            [2208] = new(2208, 200302, 1401, 1222, 350204), // 群攻概念股 (bonus 42008)
            [2209] = new(2209, 200402, 1405, 1313, 350202), // 连击概念股 (bonus 42009)
            [2210] = new(2210, 200502, 1310, 1225, 350207), // 击破概念股 (bonus 42010)
            [2211] = new(2211, 200602, 1204, 1413, 350202), // 召唤概念股 (bonus 42011)
            [2212] = new(2212, 200702, 1306, 1501, 350208), // 欢愉概念股 (bonus 42012)
            [2213] = new(2213, 200802, 1221, 1208, 350206), // 反击概念股 (bonus 42013)
            [2214] = new(2214, 200902, 1505, 1217, 350208), // 能量概念股 (bonus 42014)
        };

    private GridFightEquipmentState? _conceptStockInitialEquipment;

    public GridFightConceptStockConfig? ConceptStockConfig =>
        ConceptStockConfigs.GetValueOrDefault(SelectedPortalId);

    public uint ConceptStockBoostedTraitId => ConceptStockConfig?.TraitId ?? 0;
    public GridFightEquipmentState? ConceptStockInitialEquipment => _conceptStockInitialEquipment;

    private void InitializeConceptStockPortal()
    {
        _conceptStockInitialEquipment = null;
        var config = ConceptStockConfig;
        if (config == null) return;
        if (!GameData.GridFightEquipmentData.ContainsKey(config.InitialEquipmentId)) return;
        if (TryAddEquipment(config.InitialEquipmentId, out var equipment, AugmentEquipmentSource))
            _conceptStockInitialEquipment = equipment;
    }

    private IReadOnlyList<uint>? RollConceptStockInitialRoleIds()
    {
        var config = ConceptStockConfig;
        if (config == null) return null;

        var featured = new[] { config.FirstRoleId, config.SecondRoleId }
            .Select(avatarId => GridFightSeasonScope.ResolveRoleId(avatarId, Season))
            .Where(roleId => GameData.GridFightRoleBasicInfoData.ContainsKey(roleId) &&
                             GameData.GridFightRoleStarData.ContainsKey(roleId << 4 | 1u))
            .ToList();
        if (featured.Count != 2) return null;

        var result = new List<uint>(OfficialInitialRoleCount);
        result.AddRange(featured);
        result.AddRange(_catalog.RollConceptStockFillRoles(
                OfficialInitialRoleCount - result.Count,
                _temporarilyUnlockedRestrictedRoleIds)
            .Select(role => role.ID));
        return result.Count == OfficialInitialRoleCount ? result : null;
    }
}
