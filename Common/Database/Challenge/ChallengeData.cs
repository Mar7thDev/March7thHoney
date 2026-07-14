using MemoryPack;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.Database.Challenge;

[DbTable("Challenge")]
public class ChallengeData : BaseDatabaseDataHelper
{
    public Dictionary<int, ChallengeHistoryData> History { get; set; } = new();

    public string? ChallengeInstance { get; set; }
    public string Instance { get; set; } = ""; // placeholder

    public Dictionary<int, ChallengeGroupReward> TakenRewards { get; set; } = new();
    public Dictionary<int, List<int>> PeakTakenRewardIds { get; set; } = new();
    public Dictionary<int, ChallengePeakLevelData> PeakLevelDatas { get; set; } = new();

    public Dictionary<int, ChallengePeakBossLevelData> PeakBossLevelDatas { get; set; } = new();
    public int CurrentPeakGroupId { get; set; }

    public string? TierceInstance { get; set; }

    public Dictionary<int, ChallengeTierceHistoryData> TierceHistory { get; set; } = new();

    public void Delete(int challengeId)
    {
        History.Remove(challengeId);
    }
}

[MemoryPackable]
public partial class ChallengeTierceHistoryData
{
    public int ChallengeId { get; set; }
    public bool IsPassed { get; set; }
    public uint Stars { get; set; }
    public List<uint> FinishedTargetList { get; set; } = [];
    // Per-stage slots are initialised lazily by the manager (NormaliseSlots) to avoid
    // Newtonsoft.Json appending deserialised entries onto a non-empty default list.
    public List<List<uint>> StageLineups { get; set; } = [];
    public List<uint> StageBuffIds { get; set; } = [];
    public List<bool> StageCleared { get; set; } = [];
    public List<uint> StageScores { get; set; } = [];
    public uint UsedCycleCount { get; set; }
    public uint RecordScore { get; set; }
}

[MemoryPackable]
public partial class ChallengePeakLevelData
{
    public int LevelId { get; set; }
    public uint RoundCnt { get; set; }
    public uint PeakStar { get; set; }
    public List<uint> BaseAvatarList { get; set; } = [];
    public List<uint> FinishedTargetList { get; set; } = [];
}

[MemoryPackable]
public partial class ChallengePeakBossLevelData
{
    public int LevelId { get; set; }
    public uint BuffId { get; set; }
    public bool IsHard { get; set; }
    public uint RoundCnt { get; set; }
    public uint PeakStar { get; set; }
    public List<uint> BaseAvatarList { get; set; } = [];
    public List<uint> FinishedTargetList { get; set; } = [];
}

[MemoryPackable]
public partial class ChallengeHistoryData(int uid, int challengeId)
{
    [MemoryPackConstructor]
    public ChallengeHistoryData() : this(0, 0)
    {
    }

    public int OwnerId { get; set; } = uid;

    public int ChallengeId { get; set; } = challengeId;
    public int GroupId { get; set; }
    public int TakenReward { get; set; }
    public int Stars { get; set; }
    public int Score { get; set; }

    public void SetStars(int stars)
    {
        Stars = Math.Max(Stars, stars);
    }

    public int GetTotalStars()
    {
        var total = 0;
        for (var i = 0; i < 3; i++) total += (Stars & (1 << i)) != 0 ? 1 : 0;
        return total;
    }

    public Proto.Challenge ToProto()
    {
        var proto = new Proto.Challenge
        {
            ChallengeId = (uint)ChallengeId,
            TakenReward = (uint)TakenReward,
            ScoreId = (uint)Score,
            Star = (uint)Stars
        };

        return proto;
    }
}

[MemoryPackable]
public partial class ChallengeInstanceData
{
    public Position StartPos { get; set; } = new();
    public Position StartRot { get; set; } = new();
    public int ChallengeId { get; set; } = 0;
    public int CurrentStage { get; set; }
    public int CurrentExtraLineup { get; set; }
    public int Status { get; set; }
    public bool HasAvatarDied { get; set; }

    public int SavedMp { get; set; }
    public int RoundsLeft { get; set; }
    public int Stars { get; set; }
    public int ScoreStage1 { get; set; }
    public int ScoreStage2 { get; set; }
    public List<int> StoryBuffs { get; set; } = [];
    public List<int> BossBuffs { get; set; } = [];
}

[MemoryPackable]
public partial class ChallengeGroupReward(int uid, int groupId)
{
    [MemoryPackConstructor]
    public ChallengeGroupReward() : this(0, 0)
    {
    }

    public int OwnerUid = uid;
    public int GroupId { get; set; } = groupId;
    public long TakenStars { get; set; }

    public bool HasTakenReward(int starCount)
    {
        return (TakenStars & (1L << starCount)) != 0;
    }

    public void SetTakenReward(int starCount)
    {
        TakenStars |= 1L << starCount;
    }

    public ChallengeGroup ToProto()
    {
        var proto = new ChallengeGroup
        {
            GroupId = (uint)GroupId,
            TakenStarsCountReward = (ulong)TakenStars
        };

        return proto;
    }
}
