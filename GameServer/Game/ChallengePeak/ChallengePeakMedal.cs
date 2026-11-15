using March7thHoney.Data;
using March7thHoney.Database.Challenge;
using March7thHoney.Enums.Challenge;
using March7thHoney.Enums.Item;

namespace March7thHoney.GameServer.Game.ChallengePeak;

/// <summary>
///     王棋勋章档位与对应的限时头像框。
///     <para>
///         档位与客户端 <c>RPG.Client.ChallengePeakBoss.CalculateRank(bool, int)</c> 同口径: 达成颜色目标为彩钻,
///         否则完成 1/2/3 个普通目标为铜/银/金, 0 个为无。颜色目标就是下发给客户端的 hard_mode_has_passed
///         (结算与总览都由 <see cref="IsColorMedalClear" /> 判定)。
///     </para>
///     <para>
///         头像框从 ChallengePeakReward 读取: BOSS_STAR_LIMIT_REWARD(TypeValue = 档位)与 BOSS_COLOR_TARGET_REWARD(彩钻)
///         → RewardData → 子类型为 HeadIconFrame 的物品。客户端 <c>ChallengePeakReward._CheckRewardCanTake</c>
///         对这两类恒返回 false, 它们只能由服务端按战绩发放, 不走领奖请求。
///     </para>
/// </summary>
public static class ChallengePeakMedal
{
    public const string StarFrameRewardType = "BOSS_STAR_LIMIT_REWARD";
    public const string ColorFrameRewardType = "BOSS_COLOR_TARGET_REWARD";

    public static bool IsHeadFrameReward(string rewardType)
    {
        return rewardType is StarFrameRewardType or ColorFrameRewardType;
    }

    /// <summary>颜色目标: 绝境通关, 或普通 0 轮通关(官服抓包: 普通 0 轮下发 hard_mode_has_passed, 3 轮不下发)。</summary>
    public static bool IsColorMedalClear(bool isHard, uint roundCnt)
    {
        return isHard || roundCnt == 0;
    }

    /// <summary>按最佳战绩判定是否已达成颜色目标(两个难度各自的记录)。</summary>
    public static bool HasColorMedal(ChallengePeakBossLevelData? easyRec, ChallengePeakBossLevelData? hardRec)
    {
        return (hardRec?.HasRecord == true && IsColorMedalClear(true, hardRec.RoundCnt)) ||
               (easyRec?.HasRecord == true && IsColorMedalClear(false, easyRec.RoundCnt));
    }

    public static ChallengePeakRankEnum CalculateRank(bool colorMedalFinished, int passedTargetCount)
    {
        if (colorMedalFinished) return ChallengePeakRankEnum.ColorMedal;
        return passedTargetCount is >= (int)ChallengePeakRankEnum.Bronze and <= (int)ChallengePeakRankEnum.Gold
            ? (ChallengePeakRankEnum)passedTargetCount
            : ChallengePeakRankEnum.None;
    }

    /// <summary>王棋最佳战绩对应的勋章档位: 颜色目标优先, 否则取普通难度最佳记录完成的目标数。</summary>
    public static ChallengePeakRankEnum GetBossRank(ChallengePeakBossLevelData? easyRec,
        ChallengePeakBossLevelData? hardRec)
    {
        var easyTargets = easyRec?.HasRecord == true ? easyRec.FinishedTargetList.Count : 0;
        return CalculateRank(HasColorMedal(easyRec, hardRec), easyTargets);
    }

    /// <summary>该奖励组里某档位对应的头像框物品, 配置缺失时为 0。</summary>
    public static uint GetHeadFrameItemId(int rewardGroupId, ChallengePeakRankEnum rank)
    {
        if (rank == ChallengePeakRankEnum.None ||
            !GameData.ChallengePeakRewardData.TryGetValue(rewardGroupId, out var rewardLine))
            return 0;

        var row = rewardLine.Where(x => IsHeadFrameReward(x.RewardType))
            .OrderBy(x => x.ID)
            .FirstOrDefault(x => GetRewardRank(x.RewardType, x.TypeValue) == rank);
        return row == null ? 0 : GetHeadFrameItemOfReward(row.RewardID);
    }

    /// <summary>头像框物品属于哪一档; 不是异相仲裁头像框(含旧版拼出的无效 ID)时为 None。</summary>
    public static ChallengePeakRankEnum GetHeadFrameRank(uint itemId)
    {
        if (itemId == 0) return ChallengePeakRankEnum.None;

        var best = ChallengePeakRankEnum.None;
        foreach (var row in GameData.ChallengePeakRewardData.Values.SelectMany(x => x))
        {
            if (!IsHeadFrameReward(row.RewardType) || GetHeadFrameItemOfReward(row.RewardID) != itemId) continue;
            var rank = GetRewardRank(row.RewardType, row.TypeValue);
            if (rank > best) best = rank;
        }

        return best;
    }

    private static ChallengePeakRankEnum GetRewardRank(string rewardType, int typeValue)
    {
        return rewardType switch
        {
            ColorFrameRewardType => ChallengePeakRankEnum.ColorMedal,
            StarFrameRewardType => CalculateRank(false, typeValue),
            _ => ChallengePeakRankEnum.None
        };
    }

    private static uint GetHeadFrameItemOfReward(int rewardId)
    {
        if (!GameData.RewardDataData.TryGetValue(rewardId, out var reward)) return 0;
        foreach (var (itemId, _) in reward.GetItems())
            if (GameData.ItemConfigData.GetValueOrDefault(itemId)?.ItemSubType == ItemSubTypeEnum.HeadIconFrame)
                return (uint)itemId;
        return 0;
    }
}
