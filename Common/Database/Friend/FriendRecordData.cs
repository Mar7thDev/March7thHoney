using CastoriceHoney.Server.Proto;
using CastoriceHoney.Server.Util;
using SqlSugar;

namespace CastoriceHoney.Server.Database.Friend;

[SugarTable("friend_record_data")]
public class FriendRecordData : BaseDatabaseDataHelper
{
    [SugarColumn(IsJson = true)]
    public List<FriendDevelopmentInfoPb> DevelopmentInfos { get; set; } = []; // max 20 entries

    [SugarColumn(IsJson = true)]
    public Dictionary<uint, ChallengeGroupStatisticsPb> ChallengeGroupStatistics { get; set; } =
        []; // cur group statistics

    public uint NextRecordId { get; set; }

    public void AddAndRemoveOld(FriendDevelopmentInfoPb info)
    {
        // get same type
        var same = DevelopmentInfos.Where(x => x.DevelopmentType == info.DevelopmentType);

        // if param equal remove
        foreach (var infoPb in same.ToArray())
            // ReSharper disable once UsageOfDefaultStructEquality
            if (infoPb.Params.SequenceEqual(info.Params))
                // remove
                DevelopmentInfos.Remove(infoPb);

        DevelopmentInfos.Add(info);
    }
}

public class FriendDevelopmentInfoPb
{
    public DevelopmentType DevelopmentType { get; set; }
    public long Time { get; set; } = Extensions.GetUnixSec();
    public Dictionary<string, uint> Params { get; set; } = [];

    public FriendDevelopmentInfo ToProto()
    {
        var proto = new FriendDevelopmentInfo
        {
            Time = Time,
            DevelopmentType = DevelopmentType
        };

        // 4.2 proto names differ from legacy 3.x enum members; keep a generic mapping here.
        proto.RogueDevelopmentInfo = new FriendRogueDevelopmentInfo
        {
            AreaId = Params.GetValueOrDefault("AreaId", 0u)
        };
        proto.ChallengeDevelopmentInfo = new FriendChallengeDevelopmentInfo
        {
            ChallengeId = Params.GetValueOrDefault("ChallengeId", 0u)
        };
        proto.AvatarId = Params.GetValueOrDefault("AvatarId", 0u);
        proto.EquipmentTid = Params.GetValueOrDefault("EquipmentTid", 0u);
        proto.RogueTournDevelopmentInfo = new FriendRogueTournDevelopmentInfo
        {
            AreaId = Params.GetValueOrDefault("AreaId", 0u),
            FinishTournDifficulty = Params.GetValueOrDefault("FinishTournDifficulty", 0u)
        };
        proto.ChallengePeakDevelopmentInfo = new FriendChallengePeakDevelopmentInfo
        {
            PeakLevelId = Params.GetValueOrDefault("PeakLevelId", 0u)
        };

        return proto;
    }
}

public class ChallengeGroupStatisticsPb
{
    public uint GroupId { get; set; }
    public Dictionary<uint, MemoryGroupStatisticsPb>? MemoryGroupStatistics { get; set; }
    public Dictionary<uint, StoryGroupStatisticsPb>? StoryGroupStatistics { get; set; }
    public Dictionary<uint, BossGroupStatisticsPb>? BossGroupStatistics { get; set; }

    public ChallengeGroupStatistics ToProto()
    {
        var proto = new ChallengeGroupStatistics
        {
            GroupId = GroupId
        };

        if (MemoryGroupStatistics != null)
        {
            foreach (var memoryGroupStatistic in MemoryGroupStatistics.Values)
                proto.GroupTotalStars += memoryGroupStatistic.Stars;

            var maxFloor = MemoryGroupStatistics.Values.MaxBy(x => x.Level);
            if (maxFloor != null) proto.MemoryGroup = maxFloor.ToProto();
        }

        if (StoryGroupStatistics != null)
        {
            foreach (var storyGroupStatistic in StoryGroupStatistics.Values)
                proto.GroupTotalStars += storyGroupStatistic.Stars;

            var maxFloor = StoryGroupStatistics.Values.MaxBy(x => x.Level);
            if (maxFloor != null) proto.StoryGroup = maxFloor.ToProto();
        }

        if (BossGroupStatistics != null)
        {
            foreach (var bossGroupStatistic in BossGroupStatistics.Values)
                proto.GroupTotalStars += bossGroupStatistic.Stars;

            var maxFloor = BossGroupStatistics.Values.MaxBy(x => x.Level);
            if (maxFloor != null) proto.BossGroup = maxFloor.ToProto();
        }

        return proto;
    }
}

public class MemoryGroupStatisticsPb
{
    public uint RecordId { get; set; }
    public uint Level { get; set; }
    public uint RoundCount { get; set; }
    public uint Stars { get; set; }
    public List<List<ChallengeAvatarInfoPb>> Lineups { get; set; } = [];

    public MemoryGroupStatistics ToProto()
    {
        return new MemoryGroupStatistics
        {
            RecordId = RecordId,
            SttInfo = new MemoryStatisticsInfo
            {
                CurLevelStars = Stars,
                Level = Level,
                RoundCount = RoundCount,
                LineupList =
                {
                    Lineups.Select(x => new ChallengeLineupList
                    {
                        AvatarList = { x.Select(avatar => avatar.ToProto()) }
                    })
                }
            }
        };
    }
}

public class StoryGroupStatisticsPb
{
    public uint RecordId { get; set; }
    public uint Level { get; set; }
    public uint Score { get; set; }
    public uint BuffOne { get; set; }
    public uint BuffTwo { get; set; }
    public uint Stars { get; set; }
    public List<List<ChallengeAvatarInfoPb>> Lineups { get; set; } = [];

    public StoryGroupStatistics ToProto()
    {
        return new StoryGroupStatistics
        {
            RecordId = RecordId,
            SttInfo = new StoryStatisticsInfo
            {
                CurLevelStars = Stars,
                Level = Level,
                LineupList =
                {
                    Lineups.Select(x => new ChallengeLineupList
                    {
                        AvatarList = { x.Select(avatar => avatar.ToProto()) }
                    })
                },
                BuffOne = BuffOne,
                BuffTwo = BuffTwo,
                ScoreId = Score
            }
        };
    }
}

public class BossGroupStatisticsPb
{
    public uint RecordId { get; set; }
    public uint Level { get; set; }
    public uint Score { get; set; }
    public uint BuffOne { get; set; }
    public uint BuffTwo { get; set; }
    public uint Stars { get; set; }
    public List<List<ChallengeAvatarInfoPb>> Lineups { get; set; } = [];

    public BossGroupStatistics ToProto()
    {
        return new BossGroupStatistics
        {
            RecordId = RecordId,
            SttInfo = new BossStatisticsInfo
            {
                CurLevelStars = Stars,
                Level = Level,
                LineupList =
                {
                    Lineups.Select(x => new ChallengeLineupList
                    {
                        AvatarList = { x.Select(avatar => avatar.ToProto()) }
                    })
                },
                BuffOne = BuffOne,
                BuffTwo = BuffTwo,
                ScoreId = Score
            }
        };
    }
}

public class ChallengeAvatarInfoPb
{
    public uint Level { get; set; }
    public uint Index { get; set; }
    public uint Id { get; set; }
    public AvatarType AvatarType { get; set; } = AvatarType.AvatarFormalType;

    public ChallengeAvatarInfo ToProto()
    {
        return new ChallengeAvatarInfo
        {
            Level = Level,
            AvatarType = AvatarType,
            Id = Id,
            Index = Index
        };
    }
}
