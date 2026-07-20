using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Challenge.Definitions;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.GameServer.Server.Packet.Send.Challenge;
using March7thHoney.GameServer.Server.Packet.Send.Lineup;
using March7thHoney.Proto;
using March7thHoney.GameServer.Game.Challenge;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.Challenge.Instances;

public class ChallengeTierceInstance : BaseChallengeInstance
{
    public ChallengeMazeTierceConfigExcel Config { get; }
    public ChallengeConfigExcel PreConfig { get; }

    public uint ChallengeId => (uint)Config.ID;
    public uint CurStageIndex { get; set; }
    public bool IsSingleStage { get; set; }
    public int RoundsLeftPool { get; set; }
    public uint DeadAvatarNum { get; set; }
    public bool IsPassed { get; set; }
    public List<ChallengeTierceStageData> ResultList { get; } = [];
    public List<uint> FinishedTargetList { get; } = [];

    public Position StartPos { get; set; } = new();
    public Position StartRot { get; set; } = new();
    public int SavedMp { get; set; }

    // Snapshot of the three lineup roster the player set at StartChallenge (BaseAvatarId lists, in stage order).
    public List<List<uint>> StageLineupSnapshot { get; } = [];
    public List<uint> StageBuffIds { get; } = [];

    // Per-stage score for PF/AS Tierce (MOC keeps these zero).
    public List<int> StageScores { get; } = [0, 0, 0];
    public int GetTotalScore() => StageScores.Sum();

    // AS final settle reports only the final node's score, PF/MOC use the sum.
    public int GetRecordScore() => IsBossMode
        ? (StageScores.Count > 2 ? StageScores[2] : 0)
        : GetTotalScore();

    public bool IsStoryMode => PreConfig.IsStory();
    public bool IsBossMode => PreConfig.IsBoss();

    // Set by ChallengeEntityLoader before it calls GetStageMonsters / LoadMonster, because the
    // EntityLoader runs inside SceneInstance's ctor — Player.SceneInstance hasn't been assigned yet.
    public March7thHoney.GameServer.Game.Scene.SceneInstance? ActiveScene { get; set; }

    public ChallengeTierceInstance(PlayerInstance player, ChallengeMazeTierceConfigExcel config,
        ChallengeConfigExcel preConfig)
        : base(player, new ChallengeStateData())
    {
        Config = config;
        PreConfig = preConfig;
        RoundsLeftPool = config.ChallengeCountDown;
    }

    public override Dictionary<int, List<ChallengeConfigExcel.ChallengeMonsterInfo>> GetStageMonsters()
    {
        if (CurStageIndex == 0) return PreConfig.ChallengeMonsters1;
        if (CurStageIndex == 1) return PreConfig.ChallengeMonsters2;

        // Node 3 reads from Tierce config. Some Tierce entries (e.g. AS 30185 → MazeGroupID=6)
        // declare a group that the actual floor doesn't expose — the boss spot lives in a
        // different group on disk. Fallback: scan the floor for any group containing the
        // configured monster spot (by ConfigList[0]) and remap the dictionary key. This stays
        // version-agnostic since the lookup is driven entirely by the Excel ConfigList.
        var declared = Config.MazeGroupID;
        var floor = ActiveScene?.FloorInfo ?? Player.SceneInstance?.FloorInfo;
        if (floor != null && Config.ChallengeMonsters.TryGetValue(declared, out var monsters)
            && !floor.Groups.ContainsKey(declared) && monsters.Count > 0)
        {
            var anchorConfigId = monsters[0].ConfigId;
            foreach (var (gid, group) in floor.Groups)
            {
                if (group.MonsterList != null && group.MonsterList.Any(m => m.ID == anchorConfigId))
                {
                    return new Dictionary<int, List<ChallengeConfigExcel.ChallengeMonsterInfo>>
                    {
                        [gid] = monsters
                    };
                }
            }
        }

        return Config.ChallengeMonsters;
    }

    public int GetCurrentMazeGroupId()
    {
        return CurStageIndex switch
        {
            0 => PreConfig.MazeGroupID1,
            1 => PreConfig.MazeGroupID2,
            _ => Config.MazeGroupID
        };
    }

    public ExtraLineupType GetCurrentExtraLineupType()
    {
        return CurStageIndex switch
        {
            0 => ExtraLineupType.LineupChallenge,
            1 => ExtraLineupType.LineupChallenge2,
            _ => ExtraLineupType.LineupChallenge3
        };
    }

    public override void OnBattleStart(BattleInstance battle)
    {
        base.OnBattleStart(battle);

        if (IsStoryMode)
        {
            // PF Tierce uses a per-node turn limit (PreConfig.ChallengeCountDown comes from
            // ChallengeStoryMazeExtra.TurnLimit) and the FantasticStory score container.
            battle.RoundLimit = PreConfig.ChallengeCountDown;
            battle.AddBattleTarget(1, 10003, GetTotalScore());
            if (PreConfig.StoryExcel?.BattleTargetID != null)
                foreach (var id in PreConfig.StoryExcel.BattleTargetID)
                    battle.AddBattleTarget(5, id, GetTotalScore());
        }
        else if (IsBossMode)
        {
            // AS Tierce: same recipe as ChallengeBossInstance.OnBattleStart — no round limit
            // (fight runs until all bosses are down) and a fixed pair of type-1 score targets.
            battle.AddBattleTarget(1, 90004, 0);
            battle.AddBattleTarget(1, 90005, 0);
        }
        else
        {
            battle.RoundLimit = RoundsLeftPool;
        }

        if (PreConfig.MazeBuffID != 0)
            battle.Buffs.Add(new MazeBuff(PreConfig.MazeBuffID, 1, -1) { WaveFlag = -1 });

        // Per-node buff the player picked in the editor (StageBuffIds[CurStageIndex] comes from
        // StartChallengeTierceCsReq.stageInfoList[i].buffId).
        if (CurStageIndex < StageBuffIds.Count && StageBuffIds[(int)CurStageIndex] != 0)
            battle.Buffs.Add(new MazeBuff((int)StageBuffIds[(int)CurStageIndex], 1, -1) { WaveFlag = -1 });
    }

    public override async ValueTask OnBattleEnd(BattleInstance battle, PVEBattleResultCsReq req)
    {
        switch (req.EndStatus)
        {
            case BattleEndStatus.BattleEndWin:
                foreach (var avatar in battle.Lineup.AvatarData!.FormalAvatars)
                    if (avatar.CurrentHp <= 0)
                        DeadAvatarNum++;

                if (IsStoryMode)
                {
                    var stageScore = (int)req.Stt.ChallengeScore - GetTotalScore();
                    if (stageScore < 0) stageScore = 0;
                    StageScores[(int)CurStageIndex] = stageScore;
                }
                else if (IsBossMode)
                {
                    var stageScore = 0;
                    if (req.Stt.BattleTargetInfo.TryGetValue(1, out var targets))
                        foreach (var t in targets.BattleTargetList_)
                            stageScore += (int)t.Progress;
                    StageScores[(int)CurStageIndex] = stageScore;
                }
                else
                {
                    RoundsLeftPool = (int)Math.Max(RoundsLeftPool - req.Stt.RoundCnt, 0L);
                }
                SavedMp = Player.LineupManager!.GetCurLineup()!.Mp;

                long monsters = Player.SceneInstance!.Entities.Values.OfType<EntityMonster>().Count();
                if (monsters == 0) await OnStageCleared(req);
                break;

            case BattleEndStatus.BattleEndQuit:
                var lineup = Player.LineupManager!.GetCurLineup()!;
                lineup.Mp = SavedMp;
                await Player.MoveTo(StartPos, StartRot);
                await Player.SendPacket(new PacketSyncLineupNotify(lineup));
                break;

            default:
                var failStage = new ChallengeTierceStageData
                {
                    EndStatus = BattleEndStatus.BattleEndLose,
                    StageIndex = CurStageIndex
                };
                ResultList.Add(failStage);
                await Player.SendPacket(PacketChallengeTierceSyncNotify.ForAllStage(this));
                break;
        }
    }

    private async ValueTask OnStageCleared(PVEBattleResultCsReq req)
    {
        var stageData = new ChallengeTierceStageData
        {
            EndStatus = BattleEndStatus.BattleEndWin,
            StageIndex = CurStageIndex,
            ScoreId = (uint)Math.Max(0, StageScores[(int)CurStageIndex])
        };
        // Echo back the battle's target progress so the client renders per-node detail
        // ("对首领造成总伤害量 X%" etc.) — for AS that's the type-1 targets, for PF the type-5 list.
        if (req.Stt.BattleTargetInfo.TryGetValue(IsStoryMode ? 5u : 1u, out var targets))
            stageData.BattleTargetList.AddRange(targets.BattleTargetList_);
        ResultList.Add(stageData);

        // FinishedTargetList stays stable across all three nodes — client uses it for the
        // overall progress chips. Captured packets show this list duplicated on every sync.
        FinishedTargetList.Clear();
        foreach (var tid in Config.ChallengeTargetID) FinishedTargetList.Add((uint)tid);
        if (Config.ChallengeTierceTargetID != 0) FinishedTargetList.Add((uint)Config.ChallengeTierceTargetID);

        if (IsSingleStage)
        {
            // Single-stage replay. Captured packets show node 0/1 single-replays use
            // SingleStage settle, but a node 3 single-replay uses the AllStage settle so
            // the client renders Restart + Leave instead of "前往节点 X".
            Player.ChallengeTierceManager!.RecordStageClear(this, CurStageIndex);
            if (CurStageIndex == 2)
            {
                // AllStage settle has to carry three stage results for the client to
                // render Restart/Leave instead of "前往节点 X"; this is unconditional
                // even if the player has never cleared the other two nodes (matches the
                // captured behaviour of node 3 single-replay).
                Player.ChallengeTierceManager!.ChallengeData.TierceHistory
                    .TryGetValue((int)ChallengeId, out var history);
                var existing = ResultList.ToDictionary(r => r.StageIndex, r => r);
                for (uint i = 0; i < 3; i++)
                {
                    if (existing.ContainsKey(i)) continue;
                    ResultList.Add(new ChallengeTierceStageData
                    {
                        EndStatus = BattleEndStatus.BattleEndWin,
                        StageIndex = i,
                        ScoreId = (history != null && i < history.StageScores.Count) ? history.StageScores[(int)i] : 0u
                    });
                }
                ResultList.Sort((a, b) => a.StageIndex.CompareTo(b.StageIndex));
                IsPassed = true;
                await Player.SendPacket(PacketChallengeTierceSyncNotify.ForAllStage(this));
            }
            else
            {
                await Player.SendPacket(PacketChallengeTierceSyncNotify.ForSingleStage(this, stageData));
            }
            Player.ChallengeTierceManager!.SaveInstance();
        }
        else if (CurStageIndex < 2)
        {
            // Intermediate node — wait for client to confirm via StartChallengeTierceNextStageCsReq.
            Player.ChallengeTierceManager!.RecordStageClear(this, CurStageIndex);
            await Player.SendPacket(PacketChallengeTierceSyncNotify.ForSingleStage(this, stageData));
            Player.ChallengeTierceManager!.SaveInstance();
        }
        else
        {
            // 末关通关: 保活实例(对齐单关重打)供「重新挑战」用, 清理交给 Leave()/重开内部
            IsPassed = true;
            Player.ChallengeTierceManager!.RecordStageClear(this, CurStageIndex);
            await Player.SendPacket(PacketChallengeTierceSyncNotify.ForAllStage(this));
            await Player.SendPacket(PacketChallengeTierceSyncNotify.ForMedal((uint)PreConfig.GroupID));
            Player.ChallengeTierceManager.SaveInstance();
        }
    }
}
