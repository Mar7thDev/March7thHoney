using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Database.Avatar;
using March7thHoney.Enums.Activity;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.Activity.ExpeditionBattle;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Activity.ExpeditionBattle;

public class ExpeditionBattleManager(PlayerInstance player) : BasePlayerManager(player)
{
    private const int MaxLineupMemberCount = 4;
    private const int PassedBattleRoundCount = 100;
    private const int CarryDamageMultiplier = 3;
    private const int StageLevelDamageUnit = 1000;
    private const int PowerBaseline = 1400;
    private const int DamageJitterMin = 60;
    private const int DamageJitterMax = 140;
    private const ulong CounterTitleParamLimit = 100;

    // In-memory only by design: lineups, records and challenge count reset on relogin.
    private readonly Dictionary<int, ExpeditionBattleMapState> _maps = [];

    private ExpeditionBattleMapState? GetMap(int mapId)
    {
        if (!GameData.ExpeditionBattleMapData.ContainsKey(mapId)) return null;
        if (_maps.TryGetValue(mapId, out var state)) return state;
        state = new ExpeditionBattleMapState(mapId, GetConst("ExpeditionBattle_ChallengeLimitPerDay"));
        _maps[mapId] = state;
        return state;
    }

    private static int GetConst(string name)
    {
        return GameData.ConstValueClientData.TryGetValue(name, out var value) ? (int)value.AsUInt32() : 0;
    }

    public List<ExpeditionBattleMapData> ToProtoMaps()
    {
        return GameData.ExpeditionBattleMapData.Keys.Order().Select(ToProtoMap).ToList();
    }

    private ExpeditionBattleMapData ToProtoMap(int mapId)
    {
        var state = GetMap(mapId)!;
        var proto = new ExpeditionBattleMapData
        {
            MapId = (uint)mapId,
            RemainingChallengeCount = (uint)state.RemainingChallengeCount
        };
        proto.RouteRecordList.AddRange(state.BestRecords.Values.OrderBy(x => x.RouteId).Select(ToProtoRecord));
        return proto;
    }

    private static ExpeditionBattleRouteRecord ToProtoRecord(ExpeditionBattleRouteResult result)
    {
        var proto = new ExpeditionBattleRouteRecord
        {
            RouteId = (uint)result.RouteId,
            PassedLevel = (uint)result.PassedLevel,
            BattleRoundCount = (uint)result.BattleRoundCount,
            LastLevelPercent = (uint)result.LastLevelPercent,
            BuffValue = result.BuffValue,
            IsNewRecord = result.IsNewRecord
        };
        proto.AvatarList.AddRange(result.Avatars.Select(ToProtoAvatar));
        proto.FunList.AddRange(result.FunList.Select(ToProtoFun));
        foreach (var level in result.Levels)
        {
            var record = new ExpeditionBattleLevelRecord { LevelId = (uint)level.LevelId };
            record.AvatarDamageList.AddRange(ToProtoDamage(level.AvatarDamage));
            proto.LevelRecordList.Add(record);
        }

        return proto;
    }

    private static ExpeditionBattleAvatarInfo ToProtoAvatar(ExpeditionBattleAvatarSelection avatar)
    {
        return new ExpeditionBattleAvatarInfo { AvatarId = (uint)avatar.Id, AvatarType = avatar.AvatarType };
    }

    private static ExpeditionBattleFunRecord ToProtoFun(ExpeditionBattleFunEntry fun)
    {
        return new ExpeditionBattleFunRecord { FunId = (uint)fun.FunId, AvatarId = (uint)fun.AvatarId, FunCount = fun.FunCount };
    }

    private static IEnumerable<ExpeditionBattleAvatarDamage> ToProtoDamage(Dictionary<int, ulong> damage)
    {
        return damage.Select(x => new ExpeditionBattleAvatarDamage { AvatarId = (uint)x.Key, Damage = x.Value });
    }

    public Retcode SetLineup(int mapId, IReadOnlyList<ExpeditionBattleLineup> lineups)
    {
        var state = GetMap(mapId);
        if (state == null) return Retcode.RetStageConfigNotExist;
        var map = GameData.ExpeditionBattleMapData[mapId];
        var pending = new Dictionary<int, List<ExpeditionBattleAvatarSelection>>();
        var usedBaseAvatars = new HashSet<int>();
        foreach (var lineup in lineups)
        {
            var routeId = (int)lineup.RouteId;
            if (!map.RouteIDList.Contains(routeId) || !GameData.ExpeditionBattleRouteData.TryGetValue(routeId, out var route))
                return Retcode.RetStageConfigNotExist;
            if (lineup.AvatarList.Count > MaxLineupMemberCount) return Retcode.RetLineupInvalidMemberPos;

            var avatars = new List<ExpeditionBattleAvatarSelection>();
            foreach (var entry in lineup.AvatarList)
            {
                var selection = new ExpeditionBattleAvatarSelection((int)entry.AvatarId, entry.AvatarType);
                var (retcode, avatar) = ResolveAvatar(route, selection);
                if (avatar == null) return retcode;
                if (!usedBaseAvatars.Add(avatar.BaseAvatarId)) return Retcode.RetLineupAvatarAlreadyIn;
                avatars.Add(selection);
            }

            pending[routeId] = avatars;
        }

        state.Lineups.Clear();
        foreach (var (routeId, avatars) in pending) state.Lineups[routeId] = avatars;
        return Retcode.RetSucc;
    }

    private (Retcode, BaseAvatarInfo?) ResolveAvatar(ExpeditionBattleRouteExcel route, ExpeditionBattleAvatarSelection selection)
    {
        switch (selection.AvatarType)
        {
            case AvatarType.AvatarTrialType:
                if (!route.SpecialAvatarIDList.Contains(selection.Id)) return (Retcode.RetAvatarNotExist, null);
                var trial = Player.AvatarManager!.GetTrialAvatarByWorldLevel(selection.Id, Player.Data.WorldLevel);
                return trial == null ? (Retcode.RetAvatarNotExist, null) : (Retcode.RetSucc, trial);
            case AvatarType.AvatarFormalType:
                var formal = Player.AvatarManager!.GetFormalAvatar(selection.Id);
                return formal == null ? (Retcode.RetAvatarNotExist, null) : (Retcode.RetSucc, formal);
            default:
                return (Retcode.RetLineupAvatarTypeInvalid, null);
        }
    }

    public Retcode Start(int mapId)
    {
        var state = GetMap(mapId);
        if (state == null) return Retcode.RetStageConfigNotExist;
        if (!state.Lineups.Values.Any(x => x.Count > 0)) return Retcode.RetLineupIsEmpty;
        if (state.RemainingChallengeCount <= 0) return Retcode.RetFreqOverLimit;
        state.RemainingChallengeCount--;
        return Retcode.RetSucc;
    }

    public async ValueTask RunAndNotify(int mapId)
    {
        var state = GetMap(mapId);
        if (state == null) return;
        var map = GameData.ExpeditionBattleMapData[mapId];
        var rng = new Random(HashCode.Combine(Player.Uid, mapId, state.RemainingChallengeCount));
        var results = new List<(ExpeditionBattleRouteResult Result, ExpeditionBattleRouteExcel Route, int CarryId)>();
        foreach (var routeId in map.RouteIDList)
        {
            if (!state.Lineups.TryGetValue(routeId, out var avatars) || avatars.Count == 0) continue;
            if (!GameData.ExpeditionBattleRouteData.TryGetValue(routeId, out var route)) continue;
            var result = new ExpeditionBattleRouteResult { RouteId = routeId };
            result.Avatars.AddRange(avatars);
            if (GameData.ExpeditionBattleBuffData.TryGetValue(route.BuffID, out var buff))
                result.BuffValue = buff.BuffRank + (ulong)rng.NextInt64(0, (long)(buff.BuffRank / 4) + 1);
            results.Add((result, route, avatars[rng.Next(avatars.Count)].Id));
        }

        var turnCount = results.Count == 0 ? 0 : results.Max(x => x.Route.LevelIDList.Count);
        var funPerRound = GetConst("ExpeditionBattle_FunDataPerRound");
        for (var turn = 1; turn <= turnCount; turn++)
        {
            var notify = new ExpeditionBattleSettleScNotify { MapId = (uint)mapId, LevelTurn = (uint)turn };
            var turnDamage = new Dictionary<int, ulong>();
            foreach (var (result, route, carryId) in results)
            {
                if (turn > route.LevelIDList.Count) continue;
                var level = new ExpeditionBattleLevelResult { LevelId = route.LevelIDList[turn - 1] };
                var stageLevel = GetStageLevel(level.LevelId);
                foreach (var avatar in result.Avatars)
                {
                    var damage = SynthesizeDamage(route, avatar, stageLevel, avatar.Id == carryId, rng);
                    level.AvatarDamage[avatar.Id] = damage;
                    turnDamage[avatar.Id] = damage;
                }

                result.Levels.Add(level);
                result.PassedLevel = turn;
                result.BattleRoundCount = PassedBattleRoundCount;
                result.LastLevelPercent = 0;
                var settle = new ExpeditionBattleRouteSettleInfo
                {
                    RouteId = (uint)result.RouteId,
                    IsPassed = true,
                    BattleRoundCount = (uint)result.BattleRoundCount,
                    LastLevelPercent = (uint)result.LastLevelPercent,
                    BuffValue = result.BuffValue
                };
                settle.AvatarDamageList.AddRange(ToProtoDamage(level.AvatarDamage));
                notify.RouteSettleList.Add(settle);
            }

            notify.FunList.AddRange(PickFun(ExpeditionBattleFunTitleType.Dps, turnDamage, funPerRound, rng).Select(ToProtoFun));
            await Player.SendPacket(new PacketExpeditionBattleSettleScNotify(notify));
        }

        var isNewRecord = false;
        foreach (var (result, _, carryId) in results)
        {
            result.FunList.AddRange(PickRouteFun(result, carryId, rng));
            if (state.BestRecords.TryGetValue(result.RouteId, out var best) && !IsBetter(result, best)) continue;
            result.IsNewRecord = true;
            state.BestRecords[result.RouteId] = result;
            isNewRecord = true;
        }

        await Player.SendPacket(new PacketExpeditionBattleEndScNotify(ToProtoMap(mapId), isNewRecord));
        foreach (var record in state.BestRecords.Values) record.IsNewRecord = false;

        await Player.MissionManager!.HandleFinishType(MissionFinishTypeEnum.ExpeditionBattlePassStageCnt, mapId);
        await Player.MissionManager.HandleFinishType(MissionFinishTypeEnum.FinishQuest);
    }

    private static bool IsBetter(ExpeditionBattleRouteResult result, ExpeditionBattleRouteResult best)
    {
        if (result.PassedLevel != best.PassedLevel) return result.PassedLevel > best.PassedLevel;
        return result.BattleRoundCount > best.BattleRoundCount;
    }

    private static int GetStageLevel(int levelId)
    {
        if (!GameData.ExpeditionBattleLevelData.TryGetValue(levelId, out var level)) return 0;
        return GameData.StageConfigData.TryGetValue(level.StageID, out var stage) ? stage.Level : 0;
    }

    private ulong SynthesizeDamage(ExpeditionBattleRouteExcel route, ExpeditionBattleAvatarSelection selection, int stageLevel, bool isCarry, Random rng)
    {
        var (_, avatar) = ResolveAvatar(route, selection);
        if (avatar == null) return 0;
        var rank = avatar.GetPathInfo(selection.Id)?.Rank ?? avatar.GetCurPathInfo().Rank;
        var power = (ulong)((avatar.Level + 10 * avatar.Promotion) * (10 + rank));
        var scale = (ulong)stageLevel * (ulong)stageLevel * (ulong)StageLevelDamageUnit;
        var damage = scale * power / (ulong)PowerBaseline * (ulong)rng.Next(DamageJitterMin, DamageJitterMax + 1) / 100UL;
        return isCarry ? damage * CarryDamageMultiplier : damage;
    }

    private static List<ExpeditionBattleFunEntry> PickRouteFun(ExpeditionBattleRouteResult result, int carryId, Random rng)
    {
        var totalDamage = new Dictionary<int, ulong>();
        foreach (var level in result.Levels)
        foreach (var (avatarId, damage) in level.AvatarDamage)
            totalDamage[avatarId] = totalDamage.GetValueOrDefault(avatarId) + damage;

        var funReport = GetConst("ExpeditionBattle_FunDataReport");
        var list = PickFun(ExpeditionBattleFunTitleType.Dps, totalDamage, 1, rng);
        var supporters = result.Avatars.Where(x => x.Id != carryId).ToList();
        var assistId = supporters.Count > 0 ? supporters[rng.Next(supporters.Count)].Id : carryId;
        var assistDamage = new Dictionary<int, ulong> { [assistId] = totalDamage.GetValueOrDefault(assistId) };
        list.AddRange(PickFun(ExpeditionBattleFunTitleType.Assist, assistDamage, funReport - list.Count, rng));
        return list;
    }

    private static List<ExpeditionBattleFunEntry> PickFun(ExpeditionBattleFunTitleType type, Dictionary<int, ulong> damage, int count, Random rng)
    {
        var list = new List<ExpeditionBattleFunEntry>();
        if (damage.Count == 0 || count <= 0) return list;
        var (topId, topDamage) = damage.MaxBy(x => x.Value);
        var candidates = GameData.ExpeditionBattleFunTitleData.Values
            .Where(x => x.Type == type && IsTitleReachable(x, topDamage)).ToList();
        for (var i = 0; i < count && candidates.Count > 0; i++)
        {
            var title = candidates[rng.Next(candidates.Count)];
            candidates.Remove(title);
            list.Add(new ExpeditionBattleFunEntry(title.ID, topId, SynthesizeFunCount(title, topDamage, rng)));
        }

        return list;
    }

    // Damage-scale titles only show up when the synthesized damage actually reaches them.
    private static bool IsTitleReachable(ExpeditionBattleFunTitleExcel title, ulong topDamage)
    {
        if (title.Type != ExpeditionBattleFunTitleType.Dps) return true;
        if (title.ConditionParamType != ExpeditionBattleFunConditionType.GreaterEqual) return true;
        return title.ConditionParam <= CounterTitleParamLimit || title.ConditionParam <= topDamage;
    }

    private static ulong SynthesizeFunCount(ExpeditionBattleFunTitleExcel title, ulong topDamage, Random rng)
    {
        if (title.ConditionParamType == ExpeditionBattleFunConditionType.LessEqual) return 0;
        if (title.ConditionParam == 0) return topDamage;
        if (title.Type == ExpeditionBattleFunTitleType.Dps && title.ConditionParam > CounterTitleParamLimit) return topDamage;
        return title.ConditionParam + (ulong)rng.NextInt64(0, (long)(title.ConditionParam / 2) + 1);
    }

    public int GetMapStars(int mapId)
    {
        if (!GameData.ExpeditionBattleMapData.TryGetValue(mapId, out var map)) return 0;
        var state = GetMap(mapId)!;
        return map.RouteIDList.Sum(routeId => state.BestRecords.TryGetValue(routeId, out var best) ? best.PassedLevel : 0);
    }
}
