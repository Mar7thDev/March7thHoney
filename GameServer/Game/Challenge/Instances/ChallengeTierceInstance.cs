using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Challenge.Definitions;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.GameServer.Server.Packet.Send.Challenge;
using March7thHoney.GameServer.Server.Packet.Send.Lineup;
using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;
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

    public ChallengeTierceInstance(PlayerInstance player, ChallengeMazeTierceConfigExcel config,
        ChallengeConfigExcel preConfig)
        : base(player, new ChallengeDataPb())
    {
        Config = config;
        PreConfig = preConfig;
        RoundsLeftPool = config.ChallengeCountDown;
    }

    public override Dictionary<int, List<ChallengeConfigExcel.ChallengeMonsterInfo>> GetStageMonsters()
    {
        return CurStageIndex switch
        {
            0 => PreConfig.ChallengeMonsters1,
            1 => PreConfig.ChallengeMonsters2,
            _ => Config.ChallengeMonsters
        };
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

        battle.RoundLimit = RoundsLeftPool;

        if (PreConfig.MazeBuffID != 0)
            battle.Buffs.Add(new MazeBuff(PreConfig.MazeBuffID, 1, -1) { WaveFlag = -1 });
    }

    public override async ValueTask OnBattleEnd(BattleInstance battle, PVEBattleResultCsReq req)
    {
        switch (req.EndStatus)
        {
            case BattleEndStatus.BattleEndWin:
                foreach (var avatar in battle.Lineup.AvatarData!.FormalAvatars)
                    if (avatar.CurrentHp <= 0)
                        DeadAvatarNum++;

                RoundsLeftPool = (int)Math.Max(RoundsLeftPool - req.Stt.RoundCnt, 0L);
                SavedMp = Player.LineupManager!.GetCurLineup()!.Mp;

                long monsters = Player.SceneInstance!.Entities.Values.OfType<EntityMonster>().Count();
                if (monsters == 0) await OnStageCleared();
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

    private async ValueTask OnStageCleared()
    {
        var stageData = new ChallengeTierceStageData
        {
            EndStatus = BattleEndStatus.BattleEndWin,
            StageIndex = CurStageIndex
        };
        ResultList.Add(stageData);

        if (IsSingleStage)
        {
            // Single-stage replay — emit a single-stage settle and leave the instance in place
            // so the client can choose Restart or Leave from the result UI.
            FinishedTargetList.Clear();
            foreach (var tid in Config.ChallengeTargetID) FinishedTargetList.Add((uint)tid);
            if (Config.ChallengeTierceTargetID != 0) FinishedTargetList.Add((uint)Config.ChallengeTierceTargetID);
            Player.ChallengeTierceManager!.RecordStageClear(this, CurStageIndex);
            await Player.SendPacket(PacketChallengeTierceSyncNotify.ForSingleStage(this, stageData));
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
            // Final node cleared — settle the whole challenge.
            IsPassed = true;
            FinishedTargetList.Clear();
            foreach (var tid in Config.ChallengeTargetID) FinishedTargetList.Add((uint)tid);
            if (Config.ChallengeTierceTargetID != 0) FinishedTargetList.Add((uint)Config.ChallengeTierceTargetID);

            Player.ChallengeTierceManager!.RecordStageClear(this, CurStageIndex);
            await Player.SendPacket(PacketChallengeTierceSyncNotify.ForAllStage(this));
            Player.ChallengeTierceManager.ClearInstance();
        }
    }
}
