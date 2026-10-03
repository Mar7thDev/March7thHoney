using March7thHoney.Data;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Challenge;
using March7thHoney.GameServer.Game.Challenge.Instances;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.Proto;
using MemoryPack;

/// <summary>
///     Pure Fiction stage scores, finished/failed-run persistence for Pure Fiction, Memory of Chaos and Apocalyptic
///     Shadow, and settlement when the client omits a battle-target list (Apocalyptic Shadow, Anomaly Arbitration).
///     Battles are not fought: results are fed to the instance directly.
/// </summary>
internal static class ChallengeRegression
{
    public static async Task Run(Action<string, bool> check)
    {
        await PureFiction(check);
        await Memory(check);
        await Boss(check);
        Peak(check);
    }

    private static async Task<PlayerInstance> NewPlayer(int uid)
    {
        var player = new PlayerInstance(uid);
        await player.InitializeAsync();
        var avatars = player.AvatarManager!.Data.FormalAvatars.Select(avatar => avatar.BaseAvatarId).Distinct()
            .Take(8).ToList();
        // The two halves never share an avatar (Apocalyptic Shadow keys its stage info by avatar id).
        var half = avatars.Count / 2;
        player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupChallenge, avatars[..half]);
        player.LineupManager.SetExtraLineup(ExtraLineupType.LineupChallenge2, avatars[half..]);
        await player.LineupManager.SetExtraLineup(ExtraLineupType.LineupNone, false);
        return player;
    }

    private static ChallengeStateData? Saved(PlayerInstance player)
    {
        return player.ChallengeManager!.ChallengeData.ChallengeInstance is { } blob
            ? MemoryPackSerializer.Deserialize<ChallengeStateData>(Convert.FromBase64String(blob))
            : null;
    }

    private static async Task ClearStageMonsters(PlayerInstance player)
    {
        foreach (var monster in player.SceneInstance!.Entities.Values.OfType<EntityMonster>().ToList())
            await player.SceneInstance.RemoveEntity(monster, false);
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

        var player = await NewPlayer(60401);
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

        var saved = Saved(player)?.Story;
        check("PF final settle persists the finished state and the stage-2 score",
            saved is { CurStatus: (uint)ChallengeStatus.ChallengeFinish, CurrentStage: 2, ScoreStage2: 20000 });
        check("PF final settle records the split scores in history",
            manager.ChallengeData.History.TryGetValue(config.ID, out var history) &&
            history is { Score: 65000, ScoreOne: 45000, ScoreTwo: 20000 });
    }

    private static async Task Memory(Action<string, bool> check)
    {
        var config = GameData.ChallengeConfigData.Values
            .Where(c => !c.IsStory() && !c.IsBoss() && c.StageNum >= 2)
            .MaxBy(c => c.ID);
        check("MoC cache carries a two-stage memory challenge", config != null);
        if (config == null) return;

        var player = await NewPlayer(60403);
        var manager = player.ChallengeManager!;
        await manager.StartChallenge(config.ID, null, null);
        var memory = manager.ChallengeInstance as ChallengeMemoryInstance;
        check($"MoC {config.ID} starts on stage 1", memory is { Data.Memory.CurrentStage: 1 });
        if (memory == null) return;

        var win = new PVEBattleResultCsReq
            { EndStatus = BattleEndStatus.BattleEndWin, Stt = new BattleStatistics { RoundCnt = 1 } };
        await ClearStageMonsters(player);
        await memory.OnBattleEnd(new BattleInstance(player, player.LineupManager!.GetCurLineup()!, new List<EntityMonster>()), win);
        await ClearStageMonsters(player);
        await memory.OnBattleEnd(new BattleInstance(player, player.LineupManager.GetCurLineup()!, new List<EntityMonster>()), win);
        var state = memory.Data.Memory!;
        var saved = Saved(player)?.Memory;
        check("MoC final settle persists the finished state and remaining rounds",
            state.CurStatus == (uint)ChallengeStatus.ChallengeFinish &&
            saved is { CurStatus: (uint)ChallengeStatus.ChallengeFinish, CurrentStage: 2 } &&
            saved.RoundsLeft == state.RoundsLeft);

        await manager.StartChallenge(config.ID, null, null);
        if (manager.ChallengeInstance is ChallengeMemoryInstance failed)
            await failed.OnBattleEnd(null!, Result(BattleEndStatus.BattleEndNone, 0));
        check("MoC failed settle persists the failed state",
            Saved(player)?.Memory is { CurStatus: (uint)ChallengeStatus.ChallengeFailed });
    }

    private static async Task Boss(Action<string, bool> check)
    {
        var config = GameData.ChallengeConfigData.Values
            .Where(c => c.IsBoss() && c.StageNum >= 2)
            .MaxBy(c => c.ID);
        check("AS cache carries a two-stage boss challenge", config != null);
        if (config == null) return;

        var player = await NewPlayer(60404);
        var manager = player.ChallengeManager!;
        await manager.StartChallenge(config.ID, null, new ChallengeBossBuffInfo());
        var boss = manager.ChallengeInstance as ChallengeBossInstance;
        check($"AS {config.ID} starts on stage 1", boss is { Data.Boss.CurrentStage: 1 });
        if (boss == null) return;

        var tolerated = true;
        try
        {
            await boss.OnBattleEnd(null!, Result(BattleEndStatus.BattleEndLose, 0));
        }
        catch (KeyNotFoundException)
        {
            tolerated = false;
        }

        check("AS phase settle tolerates an omitted type-1 target list",
            tolerated && boss.Data.Boss is { ScoreStage1: 0, CurrentStage: 1 });

        var reported = Result(BattleEndStatus.BattleEndLose, 0);
        var list = new BattleTargetList();
        list.BattleTargetList_.Add(new BattleTarget { Id = 1, Progress = 5000 });
        list.BattleTargetList_.Add(new BattleTarget { Id = 2, Progress = 3000 });
        reported.Stt.BattleTargetInfo.Add(1, list);
        await boss.OnBattleEnd(null!, reported);
        check("AS phase settle still sums reported score targets", boss.Data.Boss is { ScoreStage1: 8000 });

        var quit = Result(BattleEndStatus.BattleEndQuit, 0);
        var partial = new BattleTargetList();
        partial.BattleTargetList_.Add(new BattleTarget { Id = 1, Progress = 1200 });
        quit.Stt.BattleTargetInfo.Add(1, partial);
        await boss.OnBattleEnd(null!, quit);
        check("AS quit keeps the phase score instead of the partial progress", boss.Data.Boss is { ScoreStage1: 8000 });

        await boss.OnBattleEnd(null!, Result(BattleEndStatus.BattleEndNone, 0));
        check("AS failed settle persists the failed state",
            Saved(player)?.Boss is { CurStatus: (uint)ChallengeStatus.ChallengeFailed });
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
