using March7thHoney.Proto;
using March7thHoney.Util;
using SqlSugar;

namespace March7thHoney.Database.Friend;

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

    // TODO 4.3: OHNPAFLKHNA 在 4.3 中已消失或更名，好友发展信息不在登录路径，暂时移除
}

public class ChallengeGroupStatisticsPb
{
    public uint GroupId { get; set; }
    public Dictionary<uint, MemoryGroupStatisticsPb>? MemoryGroupStatistics { get; set; }
    public Dictionary<uint, StoryGroupStatisticsPb>? StoryGroupStatistics { get; set; }
    public Dictionary<uint, BossGroupStatisticsPb>? BossGroupStatistics { get; set; }

    public object ToProto()
    {
        return new { GroupId };
    }
}

public class MemoryGroupStatisticsPb
{
    public uint RecordId { get; set; }
    public uint Level { get; set; }
    public uint RoundCount { get; set; }
    public uint Stars { get; set; }
    public List<List<ChallengeAvatarInfoPb>> Lineups { get; set; } = [];

    public object ToProto()
    {
        return new { RecordId, Level, RoundCount, Stars };
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

    public object ToProto()
    {
        return new { RecordId, Level, Score, BuffOne, BuffTwo, Stars };
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

    public object ToProto()
    {
        return new { RecordId, Level, Score, BuffOne, BuffTwo, Stars };
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
