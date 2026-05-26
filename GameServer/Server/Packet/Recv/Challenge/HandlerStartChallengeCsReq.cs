using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Challenge;

[Opcode(CmdIds.StartChallengeCsReq)]
public class HandlerStartChallengeCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = StartChallengeCsReq.Parser.ParseFrom(data);

        ChallengeStoryBuffInfo? storyBuffInfo = null;
        if (req.StageInfo is { StoryInfo: not null })
            storyBuffInfo = req.StageInfo.StoryInfo;

        ChallengeBossBuffInfo? bossBuffInfo = null;
        if (req.StageInfo != null && req.StageInfo.BossInfo != null) bossBuffInfo = req.StageInfo.BossInfo;

        // 4.3 客户端把上下半编队拆成两个 AvatarIdentifier 列表发，相对 4.2 字段
        // 语义对调（4.2 时 field 13 = 上半，4.3 抓包确认 field 1 ABNDFKFIKCI = 上半，
        // field 13 BKNKLEOCJNO = 下半）。旧版本 client 用 first_lineup / second_lineup
        // packed uint32 作 fallback。
        var firstLineup = req.ABNDFKFIKCI.Count > 0
            ? req.ABNDFKFIKCI.Select(x => (int)x.Id).Where(x => x > 0).ToList()
            : req.FirstLineup.Select(x => (int)x).Where(x => x > 0).ToList();
        if (firstLineup.Count > 0)
            await connection.Player!.LineupManager!.ReplaceLineup(0, firstLineup, ExtraLineupType.LineupChallenge);

        var secondLineup = req.BKNKLEOCJNO.Count > 0
            ? req.BKNKLEOCJNO.Select(x => (int)x.Id).Where(x => x > 0).ToList()
            : req.SecondLineup.Select(x => (int)x).Where(x => x > 0).ToList();
        if (secondLineup.Count > 0)
            await connection.Player!.LineupManager!.ReplaceLineup(0, secondLineup, ExtraLineupType.LineupChallenge2);

        await connection.Player!.ChallengeManager!.StartChallenge((int)req.ChallengeId, storyBuffInfo, bossBuffInfo);
    }
}
