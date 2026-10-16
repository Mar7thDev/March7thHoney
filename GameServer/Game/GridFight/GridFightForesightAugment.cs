using March7thHoney.Enums.GridFight;

namespace March7thHoney.GameServer.Game.GridFight;

/// <summary>
/// 远见 (303601): "获得一个随机黄金投资策略和15金币。后续的投资策略节点改为获得随机1个棱彩投资策略（无法刷新），
/// 并且不增加敌人难度。" The 15 gold comes from the selection grant table; this file owns the bonus augment,
/// the single prismatic offer and the difficulty exemption.
/// </summary>
public partial class GridFightSession
{
    private readonly List<uint> _foresightBonusAugments = [];
    private readonly HashSet<uint> _difficultyExemptInvestmentIds = [];

    public bool IsForesightActive => ActiveInvestmentIds.Contains(ForesightAugmentId);

    private void GrantForesightBonusAugment()
    {
        var bonus = _catalog.RollInvestmentOffer(ChapterId, GridFightAugmentQualityEnum.Gold, 1, ActiveInvestmentIds)
            .SingleOrDefault();
        if (bonus == 0) return;
        ActiveInvestmentIds.Add(bonus);
        _foresightBonusAugments.Add(bonus);
        PrepareInvestmentSelectionReward(bonus);
        OnP0InvestmentSelected(bonus);
        ApplyAugmentSelectionEffects(bonus);
    }

    public IReadOnlyList<uint> TakeForesightBonusAugments() => Drain(_foresightBonusAugments);

    /// <summary>Returns the one-prismatic offer while 远见 is active, or null to use the ordinary offer.</summary>
    private IReadOnlyList<uint>? RollForesightInvestmentOffer()
    {
        if (!IsForesightActive) return null;
        foreach (var quality in new[] { GridFightAugmentQualityEnum.Prismatic, GridFightAugmentQualityEnum.Gold })
        {
            var offer = _catalog.RollInvestmentOffer(ChapterId, quality, 1, ActiveInvestmentIds);
            if (offer.Count > 0) return offer;
        }
        return [];
    }

    private void OnForesightInvestmentSelected(uint augmentId)
    {
        if (augmentId != ForesightAugmentId && IsForesightActive)
            _difficultyExemptInvestmentIds.Add(augmentId);
    }
}
