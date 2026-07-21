using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Database.Activity;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.B51Racing;

public class B51RacingManager(PlayerInstance player) : BasePlayerManager<B51RacingData>(player)
{
    private const uint PlayerTeamId = 10;
    private const uint DevelopmentActionLimit = 3;
    private const string RacingRegionState = "B51RacingLevelRegionState";

    public bool IsRacingSceneReady
    {
        get
        {
            var entrance = GetRacingEntrance();
            return entrance != null && Player.Data.FloorId == entrance.FloorID &&
                   Player.SceneInstance?.FloorId == entrance.FloorID;
        }
    }

    public async ValueTask<bool> EnterRacingScene(uint trackId)
    {
        if (!GameData.B51RacingTrackData.ContainsKey((int)trackId)) return false;
        var entrance = GetRacingEntrance();
        if (entrance == null || !GameData.RaidConfigData.ContainsKey(entrance.ID * 100)) return false;

        if (IsRacingSceneReady)
        {
            await Player.SceneInstance!.UpdateFloorSavedValue(RacingRegionState, (int)trackId);
            return true;
        }

        if (!Player.SceneData!.FloorSavedData.TryGetValue(entrance.FloorID, out var floorSavedData))
        {
            floorSavedData = [];
            Player.SceneData.FloorSavedData[entrance.FloorID] = floorSavedData;
        }
        floorSavedData[RacingRegionState] = (int)trackId;

        if (Player.RaidManager!.Data.CurRaidId == entrance.ID)
        {
            await Player.EnterScene(entrance.ID, 0, true);
            return IsRacingSceneReady;
        }

        if (Player.RaidManager.Data.CurRaidId != 0) return false;

        var record = await Player.RaidManager.EnterRaid(entrance.ID, 0);
        if (record == null || !IsRacingSceneReady) return false;

        await Player.SendPacket(new PacketEnterSceneByServerScNotify(Player.SceneInstance!));
        return true;
    }

    private static MapEntranceExcel? GetRacingEntrance()
    {
        foreach (var entrance in GameData.MapEntranceData.Values)
        {
            GameData.GetFloorInfo(entrance.PlaneID, entrance.FloorID, out var floor);
            if (floor?.FloorSavedValue.Any(x => x.Name == RacingRegionState) == true) return entrance;
        }

        return null;
    }

    public B51RacingGetDataScRsp BuildGetDataResponse()
    {
        var response = new B51RacingGetDataScRsp
        {
            LastCycleId = Data.LastCycleId
        };
        if (Data.CycleId == 0) return response;

        response.Cycle = BuildCycleData();
        response.CarDisplay = BuildCarDisplay();
        if (GetCycle() is { } cycle) response.UnlockedDriverIdList.AddRange(cycle.DriverIDList.Select(x => (uint)x));
        return response;
    }

    public B51RacingStartCycleScRsp StartCycle(B51RacingStartCycleCsReq request)
    {
        if (!GameData.B51RacingCycleData.TryGetValue((int)request.CycleId, out var cycle) ||
            !GameData.B51RacingCarData.TryGetValue((int)request.CarId, out var car) ||
            request.Livery == null || request.Livery.CarNum == 0 || request.Livery.CarNum > 999 ||
            !cycle.CarIDList.Contains((int)request.CarId) ||
            !cycle.PaintIDList.Contains((int)request.Livery.PaintId))
            return new B51RacingStartCycleScRsp { Retcode = (uint)Retcode.RetReqParaInvalid };

        Data.CycleId = request.CycleId;
        Data.Day = (uint)FindNextAgendaDay(2);
        Data.CarId = request.CarId;
        Data.CarNum = request.Livery.CarNum;
        Data.PaintId = request.Livery.PaintId;
        Data.DriverId = (uint)cycle.DriverIDList.FirstOrDefault();
        Data.FinishedMatchNum = 0;
        Data.TargetPartId = 0;
        Data.DevelopmentActionCount = 0;
        Data.ActiveMatchId = 0;
        Data.ActiveTrackId = 0;
        Data.ActiveRandomSeed = 0;
        Data.EquippedPartIds = car.PartIDList.Select(x => (uint)x).ToList();
        Data.DevelopedPartIds.Clear();
        Data.SelectedDevelopmentActionIds.Clear();
        Data.StatValues = car.StatValueMap.ToDictionary(x => x.Key, x => x.Value);
        Data.MatchRecords.Clear();
        Data.TeamPoints = cycle.TeamIDList
            .Where(GameData.B51RacingTeamData.ContainsKey)
            .ToDictionary(x => (uint)x, x => (uint)Math.Max(0, GameData.B51RacingTeamData[x].InitialPoint));
        MarkDirty();

        return new B51RacingStartCycleScRsp { Cycle = BuildCycleData() };
    }

    public B51RacingDriverUnlockScNotify BuildDriverUnlockNotify()
    {
        var notify = new B51RacingDriverUnlockScNotify();
        if (GetCycle() is { } cycle) notify.DriverIdList.AddRange(cycle.DriverIDList.Select(x => (uint)x));
        return notify;
    }

    public B51RacingCarLiveryScRsp SetLivery(B51RacingCarLiveryCsReq request)
    {
        var cycle = GetCycle();
        if (cycle == null || request.Livery == null || request.Livery.CarNum == 0 ||
            !cycle.PaintIDList.Contains((int)request.Livery.PaintId))
            return new B51RacingCarLiveryScRsp { Retcode = (uint)Retcode.RetReqParaInvalid };

        Data.CarNum = request.Livery.CarNum;
        Data.PaintId = request.Livery.PaintId;
        MarkDirty();
        return new B51RacingCarLiveryScRsp { Livery = BuildLivery() };
    }

    public B51RacingSetDriverScRsp SetDriver(B51RacingSetDriverCsReq request)
    {
        var cycle = GetCycle();
        if (cycle == null || !cycle.DriverIDList.Contains((int)request.DriverId))
            return new B51RacingSetDriverScRsp { Retcode = (uint)Retcode.RetReqParaInvalid };

        Data.DriverId = request.DriverId;
        MarkDirty();
        return new B51RacingSetDriverScRsp { DriverId = request.DriverId };
    }

    public B51RacingStartMatchScRsp StartMatch(B51RacingStartMatchCsReq request)
    {
        var agenda = BuildAgenda();
        if (agenda?.Match == null || agenda.Match.MatchId != request.MatchId ||
            !GameData.B51RacingMatchData.TryGetValue((int)request.MatchId, out var match) ||
            !match.TrackIDList.Contains((int)request.TrackId) || Data.DriverId == 0)
            return new B51RacingStartMatchScRsp { Retcode = (uint)Retcode.RetReqParaInvalid };

        Data.ActiveMatchId = request.MatchId;
        Data.ActiveTrackId = request.TrackId;
        Data.ActiveRandomSeed = (uint)Random.Shared.Next(1, int.MaxValue);
        MarkDirty();

        return new B51RacingStartMatchScRsp
        {
            Match = new B51RacingMatchInfo
            {
                StartTimeMs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                MatchId = request.MatchId,
                TrackId = request.TrackId,
                RandomSeed = Data.ActiveRandomSeed
            }
        };
    }

    public B51RacingEndMatchScRsp EndMatch(B51RacingEndMatchCsReq request)
    {
        if (Data.ActiveMatchId == 0 || Data.ActiveTrackId != request.TrackId)
            return new B51RacingEndMatchScRsp { Retcode = (uint)Retcode.RetReqParaInvalid };

        var scoreByRank = new uint[] { 10, 6, 4, 3, 2, 1 };
        var rank = request.Rank == 0 ? 1u : request.Rank;
        var addedPoint = scoreByRank[Math.Min(rank, (uint)scoreByRank.Length) - 1];
        Data.TeamPoints[PlayerTeamId] = Data.TeamPoints.GetValueOrDefault(PlayerTeamId) + addedPoint;
        Data.MatchRecords.Add(new B51RacingMatchRecordData
        {
            MatchId = Data.ActiveMatchId,
            Day = Data.Day,
            TrackId = Data.ActiveTrackId,
            Rank = rank,
            FinishTimeMs = request.FinishTimeMs
        });
        Data.FinishedMatchNum++;
        Data.ActiveMatchId = 0;
        Data.ActiveTrackId = 0;
        Data.ActiveRandomSeed = 0;

        var nextDay = FindNextAgendaDay((int)Data.Day + 1);
        var isFinished = nextDay == 0;
        if (isFinished)
        {
            Data.LastCycleId = Data.CycleId;
        }
        else
        {
            Data.Day = (uint)nextDay;
            ResetDevelopmentState();
        }

        MarkDirty();
        var response = new B51RacingEndMatchScRsp
        {
            IsCycleFinished = isFinished,
            AddedTeamPoint = addedPoint,
            FinishedMatchNum = Data.FinishedMatchNum
        };
        if (!isFinished) response.Agenda = BuildAgenda();
        return response;
    }

    public B51RacingReplacePartScRsp ReplaceParts(B51RacingReplacePartCsReq request)
    {
        var allowed = GetAllowedPartIds();
        var parts = request.PartIdList.Distinct().ToList();
        if (parts.Count != 3 || parts.Any(x => !allowed.Contains(x)) ||
            parts.Select(x => GameData.B51RacingPartData[(int)x].Type).Distinct().Count() != 3)
            return new B51RacingReplacePartScRsp { Retcode = (uint)Retcode.RetReqParaInvalid };

        Data.EquippedPartIds = parts;
        MarkDirty();
        return new B51RacingReplacePartScRsp { PartInfo = BuildPartInfo() };
    }

    public B51RacingSelectDevelopPartScRsp SelectDevelopPart(B51RacingSelectDevelopPartCsReq request)
    {
        var cycle = GetCycle();
        if (cycle == null || !IsDevelopmentDay() || !cycle.PartIDList.Contains((int)request.PartId))
            return new B51RacingSelectDevelopPartScRsp { Retcode = (uint)Retcode.RetReqParaInvalid };

        Data.TargetPartId = request.PartId;
        if (!Data.DevelopedPartIds.Contains(request.PartId)) Data.DevelopedPartIds.Add(request.PartId);
        MarkDirty();
        return new B51RacingSelectDevelopPartScRsp
        {
            PartInfo = BuildPartInfo(),
            Agenda = BuildAgenda(),
            StatInfo = BuildStatInfo(),
            RemainingSelectCount = 0
        };
    }

    public B51RacingSelectDevelopActionScRsp SelectDevelopAction(B51RacingSelectDevelopActionCsReq request)
    {
        if (!IsDevelopmentDay() || Data.TargetPartId == 0 ||
            !GameData.B51RacingDevelopmentActionData.TryGetValue((int)request.ActionId, out var action) ||
            Data.SelectedDevelopmentActionIds.Contains(request.ActionId))
            return new B51RacingSelectDevelopActionScRsp { Retcode = (uint)Retcode.RetReqParaInvalid };

        foreach (var (stat, value) in action.AddStatValueMap)
            Data.StatValues[stat] = Data.StatValues.GetValueOrDefault(stat) + value;
        Data.SelectedDevelopmentActionIds.Add(request.ActionId);
        Data.DevelopmentActionCount++;

        var isFinished = Data.DevelopmentActionCount >= DevelopmentActionLimit;
        if (isFinished)
        {
            var nextDay = FindNextAgendaDay((int)Data.Day + 1);
            if (nextDay > 0) Data.Day = (uint)nextDay;
        }

        MarkDirty();
        return new B51RacingSelectDevelopActionScRsp
        {
            Agenda = BuildAgenda(),
            IsDevelopmentFinished = isFinished,
            PartInfo = BuildPartInfo(),
            RemainingActionCount = isFinished ? 0 : DevelopmentActionLimit - Data.DevelopmentActionCount,
            StatInfo = BuildStatInfo()
        };
    }

    private B51RacingCycleData BuildCycleData()
    {
        var result = new B51RacingCycleData
        {
            Agenda = BuildAgenda(),
            CarId = Data.CarId,
            Livery = BuildLivery(),
            DriverId = Data.DriverId,
            CycleId = Data.CycleId,
            PartInfo = BuildPartInfo(),
            StatInfo = BuildStatInfo(),
            FinishedMatchNum = Data.FinishedMatchNum
        };
        result.PreMatchTrackInfoList.AddRange(Data.MatchRecords.Select(x => new B51RacingPreMatchTrackInfo
        {
            MatchId = x.MatchId,
            Day = x.Day,
            TrackId = x.TrackId
        }));
        return result;
    }

    private B51RacingAgendaInfo? BuildAgenda()
    {
        if (!GameData.B51RacingAgendaData.TryGetValue((int)(Data.CycleId * 100 + Data.Day), out var agenda))
            return null;

        var result = new B51RacingAgendaInfo { Day = Data.Day };
        if (agenda.ContentType == "Match")
        {
            var matchId = GetMatchIdForDay(Data.CycleId, Data.Day);
            if (GameData.B51RacingMatchData.TryGetValue((int)matchId, out var match))
                result.Match = new B51RacingMatchInfo
                {
                    MatchId = matchId,
                    TrackId = (uint)match.TrackIDList.FirstOrDefault()
                };
        }
        else if (agenda.ContentType == "Development")
        {
            result.Development = new B51RacingDevelopmentInfo
            {
                FinishedActionNum = Data.DevelopmentActionCount
            };
            result.Development.ActivePartIdList.AddRange(GetCycle()?.PartIDList.Select(x => (uint)x) ?? []);
            result.Development.ActiveActionIdList.AddRange(GameData.B51RacingDevelopmentActionData.Keys
                .Select(x => (uint)x).Where(x => !Data.SelectedDevelopmentActionIds.Contains(x)).Order());
        }
        return result;
    }

    private B51RacingPartInfo BuildPartInfo()
    {
        var result = new B51RacingPartInfo
        {
            EquippedDecalId = (uint)(GetCycle()?.DecalIDList.FirstOrDefault() ?? 0)
        };
        result.EquippedPartIdList.AddRange(Data.EquippedPartIds);
        result.DevelopedPartIdList.AddRange(Data.DevelopedPartIds);
        return result;
    }

    private B51RacingStatInfo BuildStatInfo()
    {
        var result = new B51RacingStatInfo();
        foreach (var stat in GameData.B51RacingStatData.Values.OrderBy(x => x.GetId()))
        {
            var value = Data.StatValues.GetValueOrDefault(stat.ID);
            if (GameData.B51RacingDriverData.TryGetValue((int)Data.DriverId, out var driver))
                value += driver.AddStatMap.GetValueOrDefault(stat.ID);
            var tier = stat.TierRequireValueList.Count(x => value >= x);
            foreach (var partId in Data.EquippedPartIds)
                if (GameData.B51RacingPartData.TryGetValue((int)partId, out var part))
                    tier += part.AddStatTierMap.GetValueOrDefault(stat.ID);
            result.StatValueList.Add(new B51RacingKeyValue { Key = (uint)stat.GetId(), Value = (uint)Math.Max(0, value) });
            result.TargetStatTierList.Add(new B51RacingKeyValue { Key = (uint)stat.GetId(), Value = (uint)Math.Max(0, tier) });
        }
        return result;
    }

    private B51RacingCarDisplay BuildCarDisplay()
    {
        var result = new B51RacingCarDisplay
        {
            CarNum = Data.CarNum,
            CarId = Data.CarId,
            PaintId = Data.PaintId
        };
        result.EquippedPartIdList.AddRange(Data.EquippedPartIds);
        return result;
    }

    private B51RacingCarLivery BuildLivery() => new() { CarNum = Data.CarNum, PaintId = Data.PaintId };

    private B51RacingCycleExcel? GetCycle()
    {
        GameData.B51RacingCycleData.TryGetValue((int)Data.CycleId, out var cycle);
        return cycle;
    }

    private HashSet<uint> GetAllowedPartIds()
    {
        var result = Data.DevelopedPartIds.ToHashSet();
        if (GameData.B51RacingCarData.TryGetValue((int)Data.CarId, out var car))
            result.UnionWith(car.PartIDList.Select(x => (uint)x));
        return result;
    }

    private bool IsDevelopmentDay()
    {
        return GameData.B51RacingAgendaData.TryGetValue((int)(Data.CycleId * 100 + Data.Day), out var agenda) &&
               agenda.ContentType == "Development";
    }

    private int FindNextAgendaDay(int startDay)
    {
        for (var day = startDay; day <= 14; day++)
            if (GameData.B51RacingAgendaData.TryGetValue((int)(Data.CycleId * 100 + day), out var agenda) &&
                agenda.ContentType != "Mission") return day;
        return 0;
    }

    private static uint GetMatchIdForDay(uint cycleId, uint day)
    {
        var matchIndex = GameData.B51RacingAgendaData.Values.Count(x =>
            x.CycleID == cycleId && x.Day <= day && x.ContentType == "Match");
        return (uint)((cycleId - 1) * 20 + matchIndex);
    }

    private void ResetDevelopmentState()
    {
        Data.TargetPartId = 0;
        Data.DevelopmentActionCount = 0;
        Data.SelectedDevelopmentActionIds.Clear();
    }
}
