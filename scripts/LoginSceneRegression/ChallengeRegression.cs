using March7thHoney.Data;
using March7thHoney.GameServer.Game.Challenge;
using March7thHoney.GameServer.Game.Challenge.Instances;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Proto;
using MemoryPack;

/// <summary>
///     Pure Fiction stage scores and finished-run persistence, and Anomaly Arbitration star calculation when the
///     client omits a battle-target list. Battles are not fought: results are fed to the instance directly.
/// </summary>
internal static class ChallengeRegression
{
    public static async Task Run(Action<string, bool> check)
    {
        await PureFiction(check);
        Peak(check);
    }

    private static PVEBattleResultCsReq Result(BattleEndStatus status, uint score)
    {
        return new PVEBattleResultCsReq { EndStatus = status, Stt = new BattleStatistics { ChallengeScore = score } };
    }

    private static async Task PureFiction(Action<string, bool> check)
    {
        var config = GameData.ChallengeConfigData.Values
            .Where(c => c.IsStory() && c.StageNum >= 2 && c.StoryExcel != null)
            .MaxBy(c => c.ID);
        check("PF cache carries a two-stage story challenge", config != null);
        if (config == null) return;

        var player = new PlayerInstance(60401);
        await player.InitializeAsync();
        var avatars = player.AvatarManager!.Data.FormalAvatars.Select(avatar => avatar.BaseAvatarId).Distinct()
            .Take(4).ToList();
        player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupChallenge, avatars);
        player.LineupManager.SetExtraLineup(ExtraLineupType.LineupChallenge2, avatars);
        await player.LineupManager.SetExtraLineup(ExtraLineupType.LineupNone, false);
        var manager = player.ChallengeManager!;
        await manager.StartChallenge(config.ID, new ChallengeStoryBuffInfo(), null);
        var story = manager.ChallengeInstance as ChallengeStoryInstance;
        check($"PF {config.ID} starts on stage 1", story is { Data.Story.CurrentStage: 1 });
        if (story == null) return;

        var state = story.Data.Story!;
        // A stage-1 win with monsters left keeps the stage open; the client reports the cumulative score.
        await story.OnBattleEnd(null!, Result(BattleEndStatus.BattleEndWin, 30000));
        check("PF stage-1 win with monsters left records the score and stays on stage 1",
            state is { ScoreStage1: 30000, CurrentStage: 1 });

        await story.OnBattleEnd(null!, Result(BattleEndStatus.BattleEndQuit, 0));
        check("PF quit keeps the stage score instead of wrapping a negative delta",
            state is { ScoreStage1: 30000, ScoreStage2: 0, CurrentStage: 1 });

        // The re-fight is seeded with the run total (30000), so 45000 is the stage's cumulative score.
        await story.OnBattleEnd(null!, Result(BattleEndStatus.BattleEndLose, 45000));
        check("PF stage-1 re-fight keeps the stage's earlier score and advances",
            state is { ScoreStage1: 45000, CurrentStage: 2 });

        await story.OnBattleEnd(null!, Result(BattleEndStatus.BattleEndLose, 65000));
        check("PF stage 2 subtracts only the stage-1 score",
            state is { ScoreStage1: 45000, ScoreStage2: 20000 } && story.GetTotalScore() == 65000);

        var saved = manager.ChallengeData.ChallengeInstance is { } blob
            ? MemoryPackSerializer.Deserialize<ChallengeStateData>(Convert.FromBase64String(blob))?.Story
            : null;
        check("PF final settle persists the finished state and the stage-2 score",
            saved is { CurStatus: (uint)ChallengeStatus.ChallengeFinish, CurrentStage: 2, ScoreStage2: 20000 });
        check("PF final settle records the split scores in history",
            manager.ChallengeData.History.TryGetValue(config.ID, out var history) &&
            history is { Score: 65000, ScoreOne: 45000, ScoreTwo: 20000 });
    }

    private static void Peak(Action<string, bool> check)
    {
        var config = GameData.ChallengePeakConfigData.Values
            .Where(c => c.BossExcel == null && c.NormalTargetList.Any(GameData.BattleTargetConfigData.ContainsKey))
            .MinBy(c => c.ID);
        check("Peak cache carries a mob level with battle targets", config != null);
        if (config == null) return;

        var player = new PlayerInstance(60402);
        var peak = new ChallengePeakInstance(player, new ChallengeStateData
        {
            Peak = new PeakChallengeState { CurrentPeakLevelId = (uint)config.ID }
        });
        var targetIds = config.NormalTargetList.Where(GameData.BattleTargetConfigData.ContainsKey).ToList();
        peak.AllBattleTargets.AddRange(targetIds);

        (uint, List<uint>)? missing = null;
        try
        {
            missing = peak.CalculateStars(Result(BattleEndStatus.BattleEndWin, 0));
        }
        catch (KeyNotFoundException)
        {
        }

        check("Peak settle tolerates an omitted type-5 target list",
            missing is { Item1: 0 } && missing.Value.Item2.Count == 0);

        var reported = Result(BattleEndStatus.BattleEndWin, 0);
        var list = new BattleTargetList();
        list.BattleTargetList_.AddRange(targetIds.Select(id => new BattleTarget
        {
            Id = (uint)id,
            Progress = (uint)GameData.BattleTargetConfigData[id].TargetParam
        }));
        reported.Stt.BattleTargetInfo.Add(5, list);
        var met = peak.CalculateStars(reported);
        check("Peak settle still counts reported targets",
            met.Item1 == Math.Min(targetIds.Count, 3) && met.Item2.Count == targetIds.Count);
    }
}
