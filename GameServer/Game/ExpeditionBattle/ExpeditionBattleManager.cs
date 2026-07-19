using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Database.Activity;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.ExpeditionBattle;

public class ExpeditionBattleManager(PlayerInstance player) : BasePlayerManager<ExpeditionBattleData>(player)
{
    private const int RequiredRouteCount = 4;
    private const int CurrentStateVersion = 1;
    private const uint MaxDailyChallengeCount = 30;
    private bool _lastResultIsNewRecord;

    private ExpeditionBattleMapExcel? GetSupportedMap()
    {
        return GameData.ExpeditionBattleMapData.Values.OrderBy(x => x.ID).FirstOrDefault(IsCompleteMap);
    }

    public GetExpeditionBattleDataScRsp BuildGetDataResponse()
    {
        var response = new GetExpeditionBattleDataScRsp();
        var map = GetSupportedMap();
        if (map != null) response.MapDataList.Add(BuildMapData(map));
        return response;
    }

    public Retcode SetLineup(ExpeditionBattleSetLineupCsReq request)
    {
        var map = GetSupportedMap();
        if (map == null || request.MapId != map.ID || !ValidateLineups(map, request.LineupList))
            return Retcode.RetReqParaInvalid;

        Data.Lineups[request.MapId] = request.LineupList.Select(lineup => new ExpeditionBattleLineupData
        {
            RouteId = lineup.RouteId,
            Avatars = lineup.AvatarList.Select(avatar => new ExpeditionBattleAvatarData
            {
                AvatarId = avatar.AvatarId,
                AvatarType = (int)avatar.AvatarType
            }).ToList()
        }).ToList();
        MarkDirty();
        return Retcode.RetSucc;
    }

    public bool TryStart(uint mapId)
    {
        var map = GetSupportedMap();
        if (map == null || mapId != map.ID || GetRemainingChallengeCount() == 0 ||
            !Data.Lineups.TryGetValue(mapId, out var lineups) ||
            !ValidateLineups(map, lineups.Select(ToProto).ToList())) return false;

        Data.DailyChallengeCount++;
        MarkDirty();
        return true;
    }

    public int GetLevelCount(uint mapId)
    {
        var map = GetSupportedMap();
        if (map == null || mapId != map.ID) return 0;

        return map.RouteIDList
            .Select(routeId => GameData.ExpeditionBattleRouteData.TryGetValue(routeId, out var route)
                ? route.LevelIDList.Count
                : 0)
            .DefaultIfEmpty()
            .Min();
    }

    public ExpeditionBattleSettleScNotify BuildSettleNotify(uint mapId, int levelIndex)
    {
        var map = GetSupportedMap()!;

        var notify = new ExpeditionBattleSettleScNotify
        {
            MapId = mapId,
            LevelTurn = (uint)(levelIndex + 1)
        };

        foreach (var routeId in map.RouteIDList)
        {
            if (!GameData.ExpeditionBattleRouteData.TryGetValue(routeId, out var route)) continue;
            var lineup = Data.Lineups[mapId].First(x => x.RouteId == routeId);
            var settle = new ExpeditionBattleRouteSettleInfo
            {
                RouteId = (uint)routeId,
                IsPassed = true,
                LastLevelPercent = 10000,
                BattleRoundCount = (uint)(levelIndex + 1),
                BuffValue = (ulong)(levelIndex + 1) * 25000
            };
            settle.AvatarDamageList.AddRange(lineup.Avatars.Select((avatar, index) =>
                new ExpeditionBattleAvatarDamage
                {
                    AvatarId = avatar.AvatarId,
                    Damage = (ulong)((levelIndex + 1) * 100000 + index * 10000)
                }));
            notify.RouteSettleList.Add(settle);
        }

        return notify;
    }

    public void Complete(uint mapId)
    {
        _lastResultIsNewRecord = Data.CompletedMaps.Add(mapId);
        MarkDirty();
    }

    public ExpeditionBattleEndScNotify BuildEndNotify()
    {
        var map = GetSupportedMap()!;
        return new ExpeditionBattleEndScNotify
        {
            IsNewRecord = _lastResultIsNewRecord,
            MapData = BuildMapData(map)
        };
    }

    private ExpeditionBattleMapData BuildMapData(ExpeditionBattleMapExcel map)
    {
        var result = new ExpeditionBattleMapData
        {
            MapId = (uint)map.ID,
            RemainingChallengeCount = GetRemainingChallengeCount()
        };

        if (!Data.Lineups.TryGetValue((uint)map.ID, out var lineups)) return result;
        foreach (var lineup in lineups)
        {
            var routeRecord = new ExpeditionBattleRouteRecord
            {
                RouteId = lineup.RouteId,
                IsNewRecord = _lastResultIsNewRecord && Data.CompletedMaps.Contains((uint)map.ID)
            };
            routeRecord.AvatarList.AddRange(lineup.Avatars.Select(ToProto));

            if (Data.CompletedMaps.Contains((uint)map.ID) &&
                GameData.ExpeditionBattleRouteData.TryGetValue((int)lineup.RouteId, out var route))
            {
                routeRecord.PassedLevel = (uint)route.LevelIDList.Count;
                routeRecord.LastLevelPercent = 10000;
                routeRecord.BattleRoundCount = (uint)route.LevelIDList.Count;
                routeRecord.BuffValue = (ulong)route.LevelIDList.Count * 25000;
                routeRecord.LevelRecordList.AddRange(route.LevelIDList.Select(levelId =>
                    new ExpeditionBattleLevelRecord { LevelId = (uint)levelId }));
            }

            result.RouteRecordList.Add(routeRecord);
        }

        return result;
    }

    private uint GetRemainingChallengeCount()
    {
        RefreshDailyChallengeCount();
        return MaxDailyChallengeCount - Math.Min(Data.DailyChallengeCount, MaxDailyChallengeCount);
    }

    private void RefreshDailyChallengeCount()
    {
        EnsureState();
        var localDate = DateTimeOffset.FromUnixTimeSeconds(ServerTimeProvider.GetServerUnixSec()).ToLocalTime().Date;
        var today = localDate.Year * 10000 + localDate.Month * 100 + localDate.Day;
        if (Data.LastChallengeDate == today) return;

        Data.LastChallengeDate = today;
        Data.DailyChallengeCount = 0;
        MarkDirty();
    }

    private void EnsureState()
    {
        if (Data.StateVersion >= CurrentStateVersion) return;

        Data.CompletedMaps.Clear();
        Data.DailyChallengeCount = 0;
        Data.LastChallengeDate = 0;
        Data.StateVersion = CurrentStateVersion;
        MarkDirty();
    }

    private static bool ValidateLineups(ExpeditionBattleMapExcel map,
        ICollection<ExpeditionBattleLineup> lineups)
    {
        if (lineups.Count != RequiredRouteCount || map.RouteIDList.Count != RequiredRouteCount) return false;
        if (!lineups.Select(x => (int)x.RouteId).Order().SequenceEqual(map.RouteIDList.Order())) return false;
        if (lineups.Any(x => x.AvatarList.Count is < 1 or > 4 || x.AvatarList.Any(a => a.AvatarId == 0))) return false;
        return lineups.SelectMany(x => x.AvatarList).Select(x => (x.AvatarId, x.AvatarType)).Distinct().Count() ==
               lineups.Sum(x => x.AvatarList.Count);
    }

    private static bool IsCompleteMap(ExpeditionBattleMapExcel map)
    {
        return map.RouteIDList.Count == RequiredRouteCount && map.RouteIDList.All(routeId =>
            GameData.ExpeditionBattleRouteData.TryGetValue(routeId, out var route) &&
            route.LevelIDList.Count > 0 && route.LevelIDList.All(levelId =>
                GameData.ExpeditionBattleLevelData.TryGetValue(levelId, out var level) &&
                GameData.StageConfigData.ContainsKey(level.StageID)));
    }

    private static ExpeditionBattleLineup ToProto(ExpeditionBattleLineupData lineup)
    {
        var result = new ExpeditionBattleLineup { RouteId = lineup.RouteId };
        result.AvatarList.AddRange(lineup.Avatars.Select(ToProto));
        return result;
    }

    private static ExpeditionBattleAvatarInfo ToProto(ExpeditionBattleAvatarData avatar)
    {
        return new ExpeditionBattleAvatarInfo
        {
            AvatarId = avatar.AvatarId,
            AvatarType = (AvatarType)avatar.AvatarType
        };
    }
}
