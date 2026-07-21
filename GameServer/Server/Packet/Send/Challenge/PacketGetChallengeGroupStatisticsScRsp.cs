using March7thHoney.Data;
using March7thHoney.Database.Friend;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using Google.Protobuf.Collections;

namespace March7thHoney.GameServer.Server.Packet.Send.Challenge;

public class PacketGetChallengeGroupStatisticsScRsp : BasePacket
{
    public PacketGetChallengeGroupStatisticsScRsp(uint groupId, ChallengeGroupStatisticsPb? data) : base(
        CmdIds.GetChallengeGroupStatisticsScRsp)
    {
        var proto = new GetChallengeGroupStatisticsScRsp
        {
            GroupId = groupId,
            Retcode = 0
        };

        var hasStatistics = false;

        if (data != null)
        {
            if (data.StoryGroupStatistics != null && data.StoryGroupStatistics.Count > 0)
            {
                var top = data.StoryGroupStatistics.Values.MaxBy(x => x.Level)!;
                proto.ChallengeStory = new ChallengeStoryStatistics
                {
                    RecordId = top.RecordId,
                    PPBHLLOJNEK = new EIKPHEMHIOH
                    {
                        Level = top.Level,
                        EEJCPNAEKLJ = top.Stars,
                        BuffOne = top.BuffOne,
                        BuffTwo = top.BuffTwo,
                        ScoreId = top.Score
                    }
                };
                AddLineups(proto.ChallengeStory.PPBHLLOJNEK.LineupList, top.Lineups);
                hasStatistics = true;
            }
            else if (data.MemoryGroupStatistics != null && data.MemoryGroupStatistics.Count > 0)
            {
                var top = data.MemoryGroupStatistics.Values.MaxBy(x => x.Level)!;
                proto.ChallengeDefault = new ChallengeStatistics
                {
                    RecordId = top.RecordId,
                    PPBHLLOJNEK = new ADKJKMKBFDC
                    {
                        Level = top.Level,
                        EEJCPNAEKLJ = top.Stars,
                        RoundCount = top.RoundCount
                    }
                };
                AddLineups(proto.ChallengeDefault.PPBHLLOJNEK.LineupList, top.Lineups);
                hasStatistics = true;
            }
            else if (data.BossGroupStatistics != null && data.BossGroupStatistics.Count > 0)
            {
                var top = data.BossGroupStatistics.Values.MaxBy(x => x.Level)!;
                proto.ChallengeBoss = new ChallengeBossStatistics
                {
                    RecordId = top.RecordId,
                    PPBHLLOJNEK = new AANLJBLOOFO
                    {
                        Level = top.Level,
                        EEJCPNAEKLJ = top.Stars,
                        BuffOne = top.BuffOne,
                        BuffTwo = top.BuffTwo,
                        ScoreId = top.Score
                    }
                };
                AddLineups(proto.ChallengeBoss.PPBHLLOJNEK.LineupList, top.Lineups);
                hasStatistics = true;
            }
        }
        
        if (!hasStatistics)
            AddBossStatisticsFallback(proto, groupId);

        SetData(proto);
    }

    private static void AddBossStatisticsFallback(GetChallengeGroupStatisticsScRsp proto, uint groupId)
    {
        var top = GameData.ChallengeConfigData.Values
            .Where(x => (uint)x.GroupID == groupId && x.IsBoss())
            .MaxBy(x => x.Floor);

        if (top == null)
            return;

        proto.ChallengeBoss = new ChallengeBossStatistics
        {
            RecordId = 1,
            PPBHLLOJNEK = new AANLJBLOOFO
            {
                Level = top.Floor,
                EEJCPNAEKLJ = top.Floor,
                ScoreId = top.Floor * 1000u
            }
        };
    }

    private static void AddLineups(RepeatedField<ChallengeLineupList> target,
        List<List<ChallengeAvatarInfoPb>> source)
    {
        foreach (var lineup in source)
        {
            var lineupProto = new ChallengeLineupList();
            lineupProto.AvatarList.AddRange(lineup.Select(x => x.ToProto()));
            target.Add(lineupProto);
        }
    }
}
