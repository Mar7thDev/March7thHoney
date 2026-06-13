using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Data.Freesr;
using March7thHoney.Database;
using March7thHoney.Database.Calyx;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.Calyx;

public class CalyxOverrideManager(PlayerInstance player) : BasePlayerManager(player)
{
    private CalyxOverrideData? _data;

    public CalyxOverrideData Data => _data ??= new CalyxOverrideData { Uid = Player.Uid };
    public bool IsActive => Data is { IsActive: true, CachedJson: not null };

    public void AttachData(CalyxOverrideData data) => _data = data;

    public void Disable()
    {
        Data.IsActive = false;
        DatabaseHelper.MarkDirty(Player.Uid);
    }

    public void Enable()
    {
        Data.IsActive = true;
        Data.LoadedAtUnix = Extensions.GetUnixSec();
        DatabaseHelper.MarkDirty(Player.Uid);
    }

    public async ValueTask<BattleInstance?> BuildOverrideBattle(int cocoonId, int wave, int worldLevel)
    {
        var data = Data.CachedJson;
        var bc = data?.BattleConfig;
        if (data == null || bc == null) return null;

        StageConfigExcel? anchor = null;
        if (bc.StageId > 0)
            GameData.StageConfigData.TryGetValue(bc.StageId, out anchor);
        if (anchor == null && GameData.CocoonConfigData.TryGetValue(cocoonId * 100 + worldLevel, out var cocoonCfg))
            GameData.StageConfigData.TryGetValue(cocoonCfg.StageIDList.RandomElement(), out anchor);
        if (anchor == null) return null;

        var monsterWaves = BuildMonsterList(bc.Monsters);
        var synthetic = new StageConfigExcel
        {
            StageID = anchor.StageID,
            StageName = anchor.StageName,
            TrialAvatarList = anchor.TrialAvatarList,
            MonsterList = monsterWaves ?? anchor.MonsterList
        };

        var lineup = Player.LineupManager!.GetCurLineup()!;
        var battle = new BattleInstance(Player, lineup, [synthetic])
        {
            StaminaCost = 0,
            WorldLevel = worldLevel > 0 ? worldLevel : Player.Data.WorldLevel,
            CocoonWave = Math.Max(wave, 1),
            MappingInfoId = 0,
            RoundLimit = bc.CycleCount > 0 ? bc.CycleCount : 0,
            CustomLevel = ResolveUniformLevel(bc.Monsters),
            CalyxOverride = new CalyxOverrideContext(data)
        };

        var avatarList = lineup.BaseAvatars!.Select(item =>
                Player.SceneInstance!.AvatarInfo.Values.FirstOrDefault(x => x.AvatarInfo.AvatarId == item.BaseAvatarId))
            .OfType<AvatarSceneInfo>().ToList();
        battle.AvatarInfo = avatarList;

        ApplyChallengeStageRules(battle, bc.StageId > 0 ? bc.StageId : anchor.StageID);

        Player.BattleInstance = battle;
        Player.QuestManager!.OnBattleStart(battle);

        await ValueTask.CompletedTask;
        return battle;
    }

    // Reverse index from stage_id back to its owning ChallengeConfigExcel (PF/AS/MoC).
    // Built lazily on first use; ChallengeConfigData is fully loaded at server boot.
    private static Dictionary<int, ChallengeConfigExcel>? _stageToChallenge;

    private static Dictionary<int, ChallengeConfigExcel> GetStageToChallengeIndex()
    {
        if (_stageToChallenge != null) return _stageToChallenge;
        var idx = new Dictionary<int, ChallengeConfigExcel>();
        foreach (var config in GameData.ChallengeConfigData.Values)
        {
            void Map(int eventId)
            {
                if (eventId <= 0) return;
                idx.TryAdd(eventId, config);
                if (GameData.PlaneEventData.TryGetValue(eventId, out var pe))
                    idx.TryAdd(pe.StageID, config);
                for (var wl = 1; wl <= 6; wl++)
                    if (GameData.PlaneEventData.TryGetValue(eventId * 10 + wl, out var pe2))
                        idx.TryAdd(pe2.StageID, config);
            }
            foreach (var list in config.ChallengeMonsters1.Values)
                foreach (var m in list) Map(m.EventId);
            foreach (var list in config.ChallengeMonsters2.Values)
                foreach (var m in list) Map(m.EventId);
        }

        // 4.4 PF/AS Tierce stages live in ChallengeMazeTierce* (EventIDList = direct stage ids),
        // classified by PreChallengeMazeID -> parent ChallengeConfig (Story/Boss). The eventId->
        // planeEvent walk above never reaches them, so map them explicitly or calyx loses the
        // PF/AS score header for any Tierce stage (e.g. 4.4 AS 420494).
        foreach (var tierce in GameData.ChallengeMazeTierceConfigData.Values)
        {
            if (!GameData.ChallengeConfigData.TryGetValue(tierce.PreChallengeMazeID, out var preConfig))
                continue;
            foreach (var stageId in tierce.EventIDList)
                if (stageId > 0) idx.TryAdd(stageId, preConfig);
        }

        _stageToChallenge = idx;
        return idx;
    }

    // For freesr stages that map to a real PF/AS/MoC config, mirror the OnBattleStart
    // logic so endgame UI (score header/tail, maze buff) renders correctly in calyx mode.
    private static void ApplyChallengeStageRules(BattleInstance battle, int stageId)
    {
        if (!GetStageToChallengeIndex().TryGetValue(stageId, out var config)) return;

        if (config.MazeBuffID > 0)
            battle.Buffs.Add(new MazeBuff(config.MazeBuffID, 1, -1) { WaveFlag = -1 });

        if (config.IsStory())
        {
            // Pure Fiction / FantasticStory: id 10003 is the continuous-wave score
            // container (client settles only after all waves clear). Using legacy
            // 10002 cuts the battle short after wave 1.
            battle.AddBattleTarget(1, 10003, 0);
            if (config.StoryExcel?.BattleTargetID != null)
                foreach (var id in config.StoryExcel.BattleTargetID)
                    battle.AddBattleTarget(5, id, 0);
        }
        else if (config.IsBoss())
        {
            // Apocalyptic Shadow: score header on slot 1.
            battle.AddBattleTarget(1, 90004, 0);
            battle.AddBattleTarget(1, 90005, 0);
        }
    }

    private static List<StageMonsterList>? BuildMonsterList(List<List<FreesrMonsterEntry>>? waves)
    {
        if (waves == null || waves.Count == 0) return null;

        var result = new List<StageMonsterList>(waves.Count);
        foreach (var wave in waves)
        {
            var expanded = new List<int>(5);
            foreach (var entry in wave)
            {
                if (entry.MonsterId <= 0) continue;
                var amount = Math.Max(1, entry.Amount);
                for (var i = 0; i < amount && expanded.Count < 5; i++)
                    expanded.Add(entry.MonsterId);
            }

            result.Add(new StageMonsterList
            {
                Monster0 = expanded.ElementAtOrDefault(0),
                Monster1 = expanded.ElementAtOrDefault(1),
                Monster2 = expanded.ElementAtOrDefault(2),
                Monster3 = expanded.ElementAtOrDefault(3),
                Monster4 = expanded.ElementAtOrDefault(4)
            });
        }

        return result;
    }

    private static int ResolveUniformLevel(List<List<FreesrMonsterEntry>>? waves)
    {
        if (waves == null) return 0;
        var first = waves.SelectMany(w => w).FirstOrDefault(m => m.Level > 0);
        return first?.Level ?? 0;
    }
}

public sealed class CalyxOverrideContext(FreesrCalyxData data)
{
    public void Apply(BattleInstance battle, SceneBattleInfo proto)
    {
        if (data.BattleConfig?.Blessings != null)
            foreach (var b in data.BattleConfig.Blessings)
                battle.Buffs.Add(new MazeBuff(b.Id, Math.Max(1, b.Level), -1) { WaveFlag = -1 });

        if (data.Avatars == null) return;

        foreach (var ba in proto.BattleAvatarList)
        {
            var aid = (int)ba.Id;
            FreesrCalyxAvatar? aj = null;
            if (data.Avatars.TryGetValue(aid, out var direct)) aj = direct;
            else aj ??= data.Avatars.Values.FirstOrDefault(a => a.AvatarId == aid);
            if (aj == null) continue;

            // freesr sp_value/sp_max use 0..120 range; in-engine CurSp/MaxSp use *100 scale.
            if (aj.SpValue.HasValue && aj.SpMax is > 0)
            {
                ba.SpBar = new SpBarInfo
                {
                    CurSp = (uint)Math.Max(0, aj.SpValue.Value * 100),
                    MaxSp = (uint)(aj.SpMax!.Value * 100)
                };
            }

            if (aj.EnhancedId is { } eid && eid > 0)
                ba.EnhancedId = (uint)eid;
        }

        var lineupAvatarIds = proto.BattleAvatarList.Select(b => (int)b.Id).ToHashSet();

        // freesr `techniques` is an array of maze buff IDs to activate for this avatar.
        // Data-driven: trust the IDs verbatim, look up Lv from MazeBuffData when available.
        foreach (var (avatarKey, aj) in data.Avatars)
        {
            if (aj.Techniques == null || aj.Techniques.Count == 0) continue;
            var avatarId = aj.AvatarId > 0 ? aj.AvatarId : avatarKey;
            if (!lineupAvatarIds.Contains(avatarId)) continue;

            foreach (var buffId in aj.Techniques)
            {
                if (buffId <= 0) continue;
                var lv = GameData.MazeBuffData.Values
                    .Where(x => x.ID == buffId)
                    .Select(x => x.Lv)
                    .DefaultIfEmpty(1)
                    .Max();

                var mb = new MazeBuff(buffId, lv, -1)
                {
                    OwnerAvatarId = avatarId,
                    WaveFlag = -1
                };
                // Client uses SkillIndex=0 to recognize a technique-triggered buff
                // (matches the M7thLite convention).
                mb.DynamicValues["SkillIndex"] = 0;
                battle.Buffs.Add(mb);
            }
        }
    }
}
