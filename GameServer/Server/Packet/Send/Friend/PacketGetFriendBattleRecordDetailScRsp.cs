using System.Numerics;
using March7thHoney.Database.Friend;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Friend;

public class PacketGetFriendBattleRecordDetailScRsp : BasePacket
{
    /// <summary>
    ///     A friend's clearance records: for each challenge group the best record (most stars, then highest level),
    ///     Memory of Chaos as challenge_default, Pure Fiction as challenge_story, Apocalyptic Shadow as challenge_boss.
    /// </summary>
    public PacketGetFriendBattleRecordDetailScRsp(int uid, FriendRecordData? recordData)
        : base(CmdIds.GetFriendBattleRecordDetailScRsp)
    {
        var proto = new GetFriendBattleRecordDetailScRsp { Uid = (uint)uid };

        var groups = recordData?.ChallengeGroupStatistics.Values.AsEnumerable() ?? [];
        foreach (var group in groups.OrderBy(group => group.GroupId))
        {
            var info = new FriendChallengeClearanceInfo { GroupId = group.GroupId };
            if (Best(group.MemoryGroupStatistics?.Values, s => s.Stars, s => s.Level) is { } memory)
            {
                var stats = new ADKJKMKBFDC { Level = memory.Level, RoundCount = memory.RoundCount };
                stats.LineupList.AddRange(Lineups(memory.Lineups));
                info.ChallengeDefault = new ChallengeStatistics { RecordId = memory.RecordId, PPBHLLOJNEK = stats };
                info.LMPPDGOFOMF = memory.Level;
            }
            else if (Best(group.StoryGroupStatistics?.Values, s => s.Stars, s => s.Level) is { } story)
            {
                var stats = new EIKPHEMHIOH
                {
                    Level = story.Level, BuffOne = story.BuffOne, BuffTwo = story.BuffTwo, ScoreId = story.Score
                };
                stats.LineupList.AddRange(Lineups(story.Lineups));
                info.ChallengeStory = new ChallengeStoryStatistics { RecordId = story.RecordId, PPBHLLOJNEK = stats };
                info.LMPPDGOFOMF = story.Level;
            }
            else if (Best(group.BossGroupStatistics?.Values, s => s.Stars, s => s.Level) is { } boss)
            {
                var stats = new AANLJBLOOFO
                {
                    Level = boss.Level, BuffOne = boss.BuffOne, BuffTwo = boss.BuffTwo, ScoreId = boss.Score
                };
                stats.LineupList.AddRange(Lineups(boss.Lineups));
                info.ChallengeBoss = new ChallengeBossStatistics { RecordId = boss.RecordId, PPBHLLOJNEK = stats };
                info.LMPPDGOFOMF = boss.Level;
            }
            else
            {
                continue;
            }

            proto.EPNLMHPCDKL.Add(info);
        }

        SetData(proto);
    }

    public PacketGetFriendBattleRecordDetailScRsp(Retcode code) : base(CmdIds.GetFriendBattleRecordDetailScRsp)
    {
        var proto = new GetFriendBattleRecordDetailScRsp
        {
            Retcode = (uint)code
        };

        SetData(proto);
    }

    // Stars are a per-target bitmask; more targets wins, then the higher level.
    private static T? Best<T>(IEnumerable<T>? records, Func<T, uint> stars, Func<T, uint> level) where T : class =>
        records?.OrderByDescending(record => BitOperations.PopCount(stars(record)))
            .ThenByDescending(level)
            .FirstOrDefault();

    private static IEnumerable<ChallengeLineupList> Lineups(List<List<ChallengeAvatarInfoPb>> lineups)
    {
        foreach (var lineup in lineups)
        {
            var list = new ChallengeLineupList();
            list.AvatarList.AddRange(lineup.Select(avatar => avatar.ToProto()));
            yield return list;
        }
    }
}
