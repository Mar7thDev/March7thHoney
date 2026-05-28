using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Database;
using March7thHoney.Database.Challenge;
using March7thHoney.GameServer.Game.Challenge.Instances;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.Challenge;
using March7thHoney.GameServer.Server.Packet.Send.Lineup;
using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;
using March7thHoney.Util;
using Newtonsoft.Json;

namespace March7thHoney.GameServer.Game.Challenge;

public class ChallengeTierceManager(PlayerInstance player) : BasePlayerManager(player)
{
    public ChallengeTierceInstance? CurrentInstance { get; private set; }

    public ChallengeData ChallengeData =>
        DatabaseHelper.Instance!.GetInstanceOrCreateNew<ChallengeData>(Player.Uid);

    public async ValueTask<bool> StartChallenge(uint challengeId,
        IList<ChallengeTierceStageLineupInfo> stageInfoList)
    {
        if (!ResolveNodes((int)challengeId, out var tierceConf, out var preConf))
            return false;

        if (stageInfoList.Count < 3) return false;

        // Push the three lineups into LineupChallenge / Challenge2 / Challenge3 in stage order.
        var lineupTypes = new[] { ExtraLineupType.LineupChallenge, ExtraLineupType.LineupChallenge2, ExtraLineupType.LineupChallenge3 };
        var snapshot = new List<List<uint>>();
        var buffSnapshot = new List<uint>();
        for (var i = 0; i < 3; i++)
        {
            var avatarIds = stageInfoList[i].Lineup
                .Select(a => (int)a.Id)
                .Where(id => id > 0)
                .ToList();
            Player.LineupManager!.SetExtraLineup(lineupTypes[i], avatarIds);

            var teamLineup = Player.LineupManager.GetExtraLineup(lineupTypes[i]);
            if (teamLineup != null)
            {
                var avatars = Player.LineupManager.GetAvatarsFromTeam((int)lineupTypes[i] + 10);
                foreach (var avatar in avatars)
                {
                    avatar.AvatarInfo.SetCurHp(10000, true);
                    avatar.AvatarInfo.SetCurSp(5000, true);
                }
                teamLineup.Mp = Player.LineupManager.GetMaxMp();
            }

            snapshot.Add(avatarIds.Select(x => (uint)x).ToList());
            buffSnapshot.Add(stageInfoList[i].BuffId);
        }

        // Build instance.
        var instance = new ChallengeTierceInstance(Player, tierceConf, preConf)
        {
            CurStageIndex = 0,
            RoundsLeftPool = tierceConf.ChallengeCountDown,
            StartPos = Player.Data.Pos!,
            StartRot = Player.Data.Rot!,
            SavedMp = 0
        };
        instance.StageLineupSnapshot.AddRange(snapshot);
        instance.StageBuffIds.AddRange(buffSnapshot);

        // Persist the editor lineups right at Start, so the per-node UI keeps showing
        // them even if the player leaves before clearing all three nodes.
        PersistEditorLineups(challengeId, stageInfoList);

        CurrentInstance = instance;
        Player.ChallengeManager!.ChallengeInstance = instance;

        // Switch to the first lineup before entering scene so EntityLoader sees the right roster.
        await Player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupChallenge, false);

        // Enter the pre-battle scene.
        await Player.EnterScene(tierceConf.MapEntranceID, 0, false);

        // After scene is built we can set leave entry so client's "退出挑战" works.
        if (Player.SceneInstance != null)
            Player.SceneInstance.LeaveEntryId = GetLobbyEntryId(preConf);

        instance.SavedMp = Player.LineupManager.GetCurLineup()?.Mp ?? 0;
        instance.StartPos = Player.Data.Pos!;
        instance.StartRot = Player.Data.Rot!;

        SaveInstance();
        return true;
    }

    public async ValueTask<bool> NextStage()
    {
        if (CurrentInstance == null) return false;
        if (CurrentInstance.CurStageIndex >= 2) return false;

        CurrentInstance.CurStageIndex++;

        var lineupType = CurrentInstance.GetCurrentExtraLineupType();

        // Reset HP/SP for the next-node lineup avatars (each node gets a fresh roster).
        var avatars = Player.LineupManager!.GetAvatarsFromTeam((int)lineupType + 10);
        foreach (var avatar in avatars)
        {
            avatar.AvatarInfo.SetCurHp(10000, true);
            avatar.AvatarInfo.SetCurSp(5000, true);
        }
        var teamLineup = Player.LineupManager.GetExtraLineup(lineupType);
        if (teamLineup != null) teamLineup.Mp = Player.LineupManager.GetMaxMp();

        await Player.LineupManager.SetExtraLineup(lineupType, false);

        // Re-enter the same map entrance — Scene/EntityLoader rebuilds and pulls the new node's monster group.
        var entranceId = CurrentInstance.Config.MapEntranceID;
        await Player.EnterScene(entranceId, 0, true);

        if (Player.SceneInstance != null)
            Player.SceneInstance.LeaveEntryId = GetLobbyEntryId(CurrentInstance.PreConfig);

        CurrentInstance.StartPos = Player.Data.Pos!;
        CurrentInstance.StartRot = Player.Data.Rot!;
        CurrentInstance.SavedMp = Player.LineupManager.GetCurLineup()?.Mp ?? 0;

        SaveInstance();
        return true;
    }

    public async ValueTask<bool> StartSingleStage(uint challengeId, uint stageIndex)
    {
        if (stageIndex > 2) return false;
        if (!ResolveNodes((int)challengeId, out var tierceConf, out var preConf)) return false;

        // Resolve each node's roster independently: prefer history (saved per-stage lineup),
        // otherwise fall back to the current LineupChallenge / Challenge2 / Challenge3 the
        // player set in any earlier attempt. The targeted stage must end up with a non-empty
        // roster — empty siblings are fine (UI just hides them).
        var lineupTypes = new[] { ExtraLineupType.LineupChallenge, ExtraLineupType.LineupChallenge2, ExtraLineupType.LineupChallenge3 };
        var rosters = new List<List<uint>>();
        var buffs = new List<uint> { 0u, 0u, 0u };

        var hasHistory = ChallengeData.TierceHistory.TryGetValue((int)challengeId, out var history);

        for (var i = 0; i < 3; i++)
        {
            List<uint>? roster = null;

            if (hasHistory && i < history!.StageLineups.Count && history.StageLineups[i].Count > 0)
                roster = history.StageLineups[i].ToList();

            if (roster == null)
            {
                var existing = Player.LineupManager!.GetExtraLineup(lineupTypes[i]);
                if (existing?.BaseAvatars != null && existing.BaseAvatars.Count > 0)
                    roster = existing.BaseAvatars.Select(a => (uint)a.BaseAvatarId).ToList();
            }

            rosters.Add(roster ?? []);

            if (hasHistory && i < history!.StageBuffIds.Count)
                buffs[i] = history.StageBuffIds[i];
        }

        if (rosters[(int)stageIndex].Count == 0) return false;

        var snapshot = new List<List<uint>>();
        for (var i = 0; i < 3; i++)
        {
            // Only write the slot when we actually have a roster — never overwrite an
            // existing extra lineup with an empty list, since that would wipe data the
            // client still needs (e.g. the other two nodes' rosters for the editor UI).
            if (rosters[i].Count > 0)
            {
                var avatarIds = rosters[i].Select(x => (int)x).ToList();
                Player.LineupManager!.SetExtraLineup(lineupTypes[i], avatarIds);

                var teamLineup = Player.LineupManager.GetExtraLineup(lineupTypes[i]);
                if (teamLineup != null)
                {
                    var avatars = Player.LineupManager.GetAvatarsFromTeam((int)lineupTypes[i] + 10);
                    foreach (var avatar in avatars)
                    {
                        avatar.AvatarInfo.SetCurHp(10000, true);
                        avatar.AvatarInfo.SetCurSp(5000, true);
                    }
                    teamLineup.Mp = Player.LineupManager.GetMaxMp();
                }
            }
            snapshot.Add(rosters[i]);
        }

        var instance = new ChallengeTierceInstance(Player, tierceConf, preConf)
        {
            CurStageIndex = stageIndex,
            IsSingleStage = true,
            RoundsLeftPool = tierceConf.ChallengeCountDown,
            SavedMp = 0,
            StartPos = Player.Data.Pos!,
            StartRot = Player.Data.Rot!
        };
        instance.StageLineupSnapshot.AddRange(snapshot);
        instance.StageBuffIds.AddRange(buffs);

        CurrentInstance = instance;
        Player.ChallengeManager!.ChallengeInstance = instance;

        await Player.LineupManager!.SetExtraLineup(instance.GetCurrentExtraLineupType(), false);
        await Player.EnterScene(tierceConf.MapEntranceID, 0, false);

        if (Player.SceneInstance != null)
            Player.SceneInstance.LeaveEntryId = GetLobbyEntryId(preConf);

        instance.SavedMp = Player.LineupManager.GetCurLineup()?.Mp ?? 0;
        instance.StartPos = Player.Data.Pos!;
        instance.StartRot = Player.Data.Rot!;

        SaveInstance();
        return true;
    }

    public async ValueTask Leave()
    {
        int entry;
        if (CurrentInstance != null)
            entry = GetLobbyEntryId(CurrentInstance.PreConfig);
        else if (Player.SceneInstance is { LeaveEntryId: var leave } && leave != 0)
            entry = leave;
        else
            entry = GameConstants.CHALLENGE_ENTRANCE;

        ClearInstance();
        await Player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupNone);
        await Player.EnterScene(entry, 0, true);
    }

    public async ValueTask<bool> Restart()
    {
        if (CurrentInstance == null) return false;

        var challengeId = CurrentInstance.ChallengeId;
        var isSingleStage = CurrentInstance.IsSingleStage;
        var stageIndex = CurrentInstance.CurStageIndex;
        var lineupSnapshot = CurrentInstance.StageLineupSnapshot
            .Select(list => new ChallengeTierceStageLineupInfo
            {
                Lineup = { list.Select(id => new AvatarIdentifier { Id = id, AvatarType = AvatarType.AvatarFormalType }) }
            })
            .ToList();
        var buffSnapshot = CurrentInstance.StageBuffIds.ToList();
        for (var i = 0; i < lineupSnapshot.Count && i < buffSnapshot.Count; i++)
            lineupSnapshot[i].BuffId = buffSnapshot[i];

        ClearInstance();

        if (isSingleStage)
            return await StartSingleStage(challengeId, stageIndex);
        return await StartChallenge(challengeId, lineupSnapshot);
    }

    public void SaveInstance()
    {
        if (CurrentInstance == null) return;
        var snap = new ChallengeTierceRuntimeSnapshot
        {
            ChallengeId = CurrentInstance.ChallengeId,
            CurStageIndex = CurrentInstance.CurStageIndex,
            IsSingleStage = CurrentInstance.IsSingleStage,
            RoundsLeftPool = CurrentInstance.RoundsLeftPool,
            DeadAvatarNum = CurrentInstance.DeadAvatarNum,
            SavedMp = CurrentInstance.SavedMp,
            StartPosX = CurrentInstance.StartPos.X,
            StartPosY = CurrentInstance.StartPos.Y,
            StartPosZ = CurrentInstance.StartPos.Z,
            StartRotX = CurrentInstance.StartRot.X,
            StartRotY = CurrentInstance.StartRot.Y,
            StartRotZ = CurrentInstance.StartRot.Z,
            StageEndStatuses = CurrentInstance.ResultList.Select(r => (int)r.EndStatus).ToList(),
            StageLineupSnapshot = CurrentInstance.StageLineupSnapshot.Select(l => l.ToList()).ToList(),
            StageBuffIds = CurrentInstance.StageBuffIds.ToList(),
            StageScores = CurrentInstance.StageScores.ToList()
        };
        ChallengeData.TierceInstance = JsonConvert.SerializeObject(snap);
    }

    public void ClearInstance()
    {
        CurrentInstance = null;
        ChallengeData.TierceInstance = null;
        if (Player.ChallengeManager != null && Player.ChallengeManager.ChallengeInstance is ChallengeTierceInstance)
            Player.ChallengeManager.ChallengeInstance = null;
    }

    public void ResurrectInstance()
    {
        if (string.IsNullOrEmpty(ChallengeData.TierceInstance)) return;

        ChallengeTierceRuntimeSnapshot? snap;
        try
        {
            snap = JsonConvert.DeserializeObject<ChallengeTierceRuntimeSnapshot>(ChallengeData.TierceInstance);
        }
        catch
        {
            ChallengeData.TierceInstance = null;
            return;
        }
        if (snap == null) return;

        if (!ResolveNodes((int)snap.ChallengeId, out var tierceConf, out var preConf))
        {
            ChallengeData.TierceInstance = null;
            return;
        }

        var instance = new ChallengeTierceInstance(Player, tierceConf, preConf)
        {
            CurStageIndex = snap.CurStageIndex,
            IsSingleStage = snap.IsSingleStage,
            RoundsLeftPool = snap.RoundsLeftPool,
            DeadAvatarNum = snap.DeadAvatarNum,
            SavedMp = snap.SavedMp,
            StartPos = new Position(snap.StartPosX, snap.StartPosY, snap.StartPosZ),
            StartRot = new Position(snap.StartRotX, snap.StartRotY, snap.StartRotZ)
        };
        instance.StageLineupSnapshot.AddRange(snap.StageLineupSnapshot);
        instance.StageBuffIds.AddRange(snap.StageBuffIds);
        for (var i = 0; i < 3 && i < snap.StageScores.Count; i++)
            instance.StageScores[i] = snap.StageScores[i];
        for (var i = 0; i < snap.StageEndStatuses.Count; i++)
        {
            instance.ResultList.Add(new ChallengeTierceStageData
            {
                EndStatus = (BattleEndStatus)snap.StageEndStatuses[i],
                StageIndex = (uint)i
            });
        }

        CurrentInstance = instance;
        Player.ChallengeManager!.ChallengeInstance = instance;
    }

    public void PersistEditorLineups(uint challengeId, IList<ChallengeTierceStageLineupInfo> stageInfoList)
    {
        if (stageInfoList.Count == 0) return;
        if (!GameData.ChallengeMazeTierceConfigData.ContainsKey((int)challengeId)) return;

        if (!ChallengeData.TierceHistory.TryGetValue((int)challengeId, out var history))
        {
            history = new ChallengeTierceHistoryData { ChallengeId = (int)challengeId };
            ChallengeData.TierceHistory[(int)challengeId] = history;
        }

        while (history.StageLineups.Count < 3) history.StageLineups.Add([]);
        while (history.StageBuffIds.Count < 3) history.StageBuffIds.Add(0u);
        while (history.StageCleared.Count < 3) history.StageCleared.Add(false);

        for (var i = 0; i < stageInfoList.Count && i < 3; i++)
        {
            var ids = stageInfoList[i].Lineup
                .Select(a => (uint)a.Id)
                .Where(id => id > 0)
                .ToList();
            if (ids.Count > 0)
                history.StageLineups[i] = ids;
            history.StageBuffIds[i] = stageInfoList[i].BuffId;
        }
    }

    public void RecordStageClear(ChallengeTierceInstance inst, uint stageIndex)
    {
        if (stageIndex > 2) return;

        if (!ChallengeData.TierceHistory.TryGetValue((int)inst.ChallengeId, out var history))
        {
            history = new ChallengeTierceHistoryData { ChallengeId = (int)inst.ChallengeId };
            ChallengeData.TierceHistory[(int)inst.ChallengeId] = history;
        }

        while (history.StageLineups.Count < 3) history.StageLineups.Add([]);
        while (history.StageBuffIds.Count < 3) history.StageBuffIds.Add(0u);
        while (history.StageCleared.Count < 3) history.StageCleared.Add(false);
        while (history.StageScores.Count < 3) history.StageScores.Add(0u);

        history.StageCleared[(int)stageIndex] = true;
        if (stageIndex < inst.StageLineupSnapshot.Count)
            history.StageLineups[(int)stageIndex] = inst.StageLineupSnapshot[(int)stageIndex].ToList();
        if (stageIndex < inst.StageBuffIds.Count)
            history.StageBuffIds[(int)stageIndex] = inst.StageBuffIds[(int)stageIndex];
        if (stageIndex < inst.StageScores.Count)
            history.StageScores[(int)stageIndex] = (uint)Math.Max(0, inst.StageScores[(int)stageIndex]);

        if (history.StageCleared.All(c => c))
            history.IsPassed = true;

        if (inst.IsPassed)
        {
            history.IsPassed = true;
            history.Stars = (uint)inst.FinishedTargetList.Count;
            history.FinishedTargetList = inst.FinishedTargetList.ToList();
            history.UsedCycleCount = (uint)Math.Max(0, inst.Config.ChallengeCountDown - inst.RoundsLeftPool);
        }
    }

    public List<ChallengeTierceData> BuildHistorySnapshots()
    {
        var list = new List<ChallengeTierceData>();
        foreach (var (challengeId, h) in ChallengeData.TierceHistory)
            list.Add(BuildHistorySnapshot(h));
        return list;
    }

    public ChallengeTierceData? BuildHistorySnapshotFor(int challengeId)
    {
        return ChallengeData.TierceHistory.TryGetValue(challengeId, out var h) ? BuildHistorySnapshot(h) : null;
    }

    private static ChallengeTierceData BuildHistorySnapshot(ChallengeTierceHistoryData h)
    {
        var data = new ChallengeTierceData
        {
            ChallengeId = (uint)h.ChallengeId,
            IsPassed = h.IsPassed,
            Record = new ChallengeTierceRecord { UsedCycleCount = h.UsedCycleCount }
        };
        data.FinishedTargetList.AddRange(h.FinishedTargetList);

        // Always emit three stage info slots (StageInfoList[i].StageIndex = i), so the client
        // places per-node rosters into the correct UI slot. Empty slots stay empty.
        for (var i = 0; i < 3; i++)
        {
            var info = new ChallengeTierceStageInfo { StageIndex = (uint)i };
            if (i < h.StageBuffIds.Count) info.BuffId = h.StageBuffIds[i];
            if (i < h.StageLineups.Count)
            {
                foreach (var id in h.StageLineups[i])
                {
                    info.Lineup.Add(new AvatarIdentifier { Id = id, AvatarType = AvatarType.AvatarFormalType });
                    info.PeakAvatarIdList.Add(id);
                }
            }
            data.StageInfoList.Add(info);
        }

        // If the whole tierce is passed, every node counts as cleared regardless of which
        // single-stage replays the player has run after. Otherwise honour per-stage flags.
        if (h.IsPassed)
        {
            for (var i = 0; i < 3; i++)
                data.ResultList.Add(new ChallengeTierceStageData
                {
                    EndStatus = BattleEndStatus.BattleEndWin,
                    StageIndex = (uint)i,
                    ScoreId = i < h.StageScores.Count ? h.StageScores[i] : 0u
                });
        }
        else
        {
            for (var i = 0; i < h.StageCleared.Count; i++)
                if (h.StageCleared[i])
                    data.ResultList.Add(new ChallengeTierceStageData
                    {
                        EndStatus = BattleEndStatus.BattleEndWin,
                        StageIndex = (uint)i,
                        ScoreId = i < h.StageScores.Count ? h.StageScores[i] : 0u
                    });
        }

        return data;
    }

    private static int GetLobbyEntryId(ChallengeConfigExcel preConfig)
    {
        if (preConfig.IsStory()) return GameConstants.CHALLENGE_STORY_ENTRANCE;
        if (preConfig.IsBoss()) return GameConstants.CHALLENGE_BOSS_ENTRANCE;
        return GameConstants.CHALLENGE_ENTRANCE;
    }

    private static bool ResolveNodes(int challengeId, out ChallengeMazeTierceConfigExcel tierceConf,
        out ChallengeConfigExcel preConf)
    {
        tierceConf = null!;
        preConf = null!;
        if (!GameData.ChallengeMazeTierceConfigData.TryGetValue(challengeId, out var t)) return false;
        if (!GameData.ChallengeConfigData.TryGetValue(t.PreChallengeMazeID, out var p)) return false;
        tierceConf = t;
        preConf = p;
        return true;
    }

    private class ChallengeTierceRuntimeSnapshot
    {
        public uint ChallengeId { get; set; }
        public uint CurStageIndex { get; set; }
        public bool IsSingleStage { get; set; }
        public int RoundsLeftPool { get; set; }
        public uint DeadAvatarNum { get; set; }
        public int SavedMp { get; set; }
        public int StartPosX { get; set; }
        public int StartPosY { get; set; }
        public int StartPosZ { get; set; }
        public int StartRotX { get; set; }
        public int StartRotY { get; set; }
        public int StartRotZ { get; set; }
        public List<int> StageEndStatuses { get; set; } = [];
        public List<List<uint>> StageLineupSnapshot { get; set; } = [];
        public List<uint> StageBuffIds { get; set; } = [];
        public List<int> StageScores { get; set; } = [];
    }
}
