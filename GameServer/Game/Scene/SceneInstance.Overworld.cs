using March7thHoney.Data;
using March7thHoney.Data.Config.Scene;
using March7thHoney.Database;
using March7thHoney.Database.Scene;
using March7thHoney.Enums.Mission;
using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Game.DailyActive;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.Util;
using Newtonsoft.Json.Linq;

namespace March7thHoney.GameServer.Game.Scene;

public partial class SceneInstance
{
    private static readonly PlaneTypeEnum[] InstanceRunPlanes =
    [
        PlaneTypeEnum.Challenge, PlaneTypeEnum.Rogue, PlaneTypeEnum.Raid,
        PlaneTypeEnum.AetherDivide, PlaneTypeEnum.TrialActivity
    ];

    private static HashSet<(int FloorId, int GroupId, int InstId)>? _chestMonsters;

    public bool IsInstanceRunPlane => InstanceRunPlanes.Contains(Excel.PlaneType);

    /// <summary>
    ///     Ordinary overworld enemies stay dead until the daily reset. Chest guardians, trotters and mission
    ///     fights stay dead. Instanced runs never consult the ledger.
    /// </summary>
    public bool MonsterStaysDefeated(GroupInfo group, MonsterInfo info)
    {
        if (IsInstanceRunPlane || Player.SceneData == null) return false;

        var permanent = MonsterDefeatIsPermanent(group, info.ID);
        var record = Player.SceneData.DefeatedMonsters.Find(row =>
            row.FloorId == FloorId && row.DimensionId == ResolveDimensionId() &&
            row.GroupId == group.Id && row.InstId == info.ID);
        if (record == null) return false;

        if (permanent && !record.Permanent)
        {
            record.Permanent = true;
            DatabaseHelper.MarkDirty(Player.Uid);
        }

        if (record.Permanent) return true;
        return record.DayKey == DailyResetClock.Today(Player.Data);
    }

    public void NoteMonsterDefeated(GroupInfo group, int instId)
    {
        if (IsInstanceRunPlane || Player.SceneData == null) return;

        var dimensionId = ResolveDimensionId();
        var permanent = MonsterDefeatIsPermanent(group, instId);
        var day = DailyResetClock.Today(Player.Data);
        // Ordinary marks from an earlier day no longer hide anything; drop them so the ledger stays small.
        Player.SceneData.DefeatedMonsters.RemoveAll(row => !row.Permanent && row.DayKey != day);
        var record = Player.SceneData.DefeatedMonsters.Find(row =>
            row.FloorId == FloorId && row.DimensionId == dimensionId &&
            row.GroupId == group.Id && row.InstId == instId);
        if (record == null)
        {
            Player.SceneData.DefeatedMonsters.Add(new DefeatedMonsterRecord
            {
                FloorId = FloorId,
                DimensionId = dimensionId,
                GroupId = group.Id,
                InstId = instId,
                Permanent = permanent,
                DayKey = day
            });
        }
        else
        {
            record.Permanent |= permanent;
            record.DayKey = day;
        }

        DatabaseHelper.MarkDirty(Player.Uid);
    }

    /// <summary>
    ///     Pad-puzzle groups name their hidden chest in ValueSource "Chest". Reveal it once the puzzle is used.
    /// </summary>
    public async ValueTask RevealAuthoredChest(GroupInfo group)
    {
        if (group.ChestRevealGroupId == 0 || group.ChestRevealInstId == 0) return;

        var chest = FindProp(group.ChestRevealGroupId, group.ChestRevealInstId);
        if (chest == null || chest.State is not (PropStateEnum.Hidden or PropStateEnum.ChestLocked)) return;
        await chest.SetState(PropStateEnum.ChestClosed);
    }

    /// <summary>
    ///     Chest-guardian groups open their ChestLocked treasures when the guarding monster dies.
    /// </summary>
    public async ValueTask UnlockTreasureOnMonsterDie(GroupInfo group)
    {
        if (!group.OpensTreasureOnMonsterDie()) return;
        foreach (var chest in GetEntitiesInGroup<EntityProp>(group.Id))
        {
            if (chest.PropInfo.ChestID == 0 || chest.State != PropStateEnum.ChestLocked) continue;
            await chest.SetState(PropStateEnum.ChestClosed);
        }
    }

    /// <summary>
    ///     A rogue scene is rebuilt from the level, so a prop whose unlocking step already finished comes back
    ///     Locked. Move those props to the state the finished step landed on, and reopen the walls that watch them.
    /// </summary>
    public async ValueTask ReapplyFinishedRoguePropUnlocks()
    {
        if (Excel.PlaneType != PlaneTypeEnum.Rogue || Player.MissionManager == null) return;

        foreach (var subId in Player.MissionManager.Data.FinishedSubMissionIds)
        {
            if (!GameData.SubMissionData.TryGetValue(subId, out var excel)) continue;
            var info = excel.SubMissionInfo;
            if (info == null) continue;
            if (info.WayPointFloorID != 0 && info.WayPointFloorID != FloorId) continue;
            if (info.LevelFloorID != 0 && info.LevelFloorID != FloorId) continue;

            var groupId = info.WayPointGroupID;
            var instId = info.WayPointEntityID;
            if (info.FinishType == MissionFinishTypeEnum.PropState && info.ParamInt1 != 0 && info.ParamInt2 != 0)
            {
                groupId = info.ParamInt1;
                instId = info.ParamInt2;
            }

            if (groupId == 0 || instId == 0) continue;

            var prop = FindProp(groupId, instId);
            if (prop is { State: PropStateEnum.Locked })
            {
                var landed = prop.PropInfo.ArmsOnlyFromOpen() && prop.PropInfo.State == PropStateEnum.Locked
                    ? PropStateEnum.Open
                    : PropStateEnum.Closed;
                await prop.SetState(landed, false);
            }

            await OpenDeathMarkedProps(groupId, instId);
        }
    }

    private async ValueTask OpenDeathMarkedProps(int groupId, int instId)
    {
        var marker = $"{groupId},{instId}";
        foreach (var prop in Entities.Values.OfType<EntityProp>())
        {
            if (prop.State == PropStateEnum.Open) continue;
            var watches = prop.GroupId == groupId && prop.InstId == instId &&
                          prop.PropInfo.InitLevelGraph?.Contains("TriggerBuffSelect", StringComparison.Ordinal) == true;
            if (!watches && prop.PropInfo.DeathMarkGroupId == groupId && prop.PropInfo.DeathMarkInstId == instId)
                watches = true;
            // The stored pair is the watched prop. Also match the literal "group,inst" captured at load.
            if (!watches && prop.PropInfo.DeathMarkGroupId != 0 &&
                $"{prop.PropInfo.DeathMarkGroupId},{prop.PropInfo.DeathMarkInstId}" == marker)
                watches = true;
            if (!watches) continue;
            await prop.SetState(PropStateEnum.Open, false);
        }
    }

    private bool MonsterDefeatIsPermanent(GroupInfo group, int instId)
    {
        if (group.IsMissionFight() || group.OpensTreasureOnMonsterDie()) return true;
        return ChestMonsterIndex().Contains((FloorId, group.Id, instId));
    }

    private static HashSet<(int FloorId, int GroupId, int InstId)> ChestMonsterIndex()
    {
        if (_chestMonsters != null) return _chestMonsters;

        var index = new HashSet<(int, int, int)>();
        var path = ConfigManager.Config.Path.ExcelOutputDirs
            .Select(dir => Path.Combine(ConfigManager.Config.Path.ResourcePath, dir, "ChestMonster.json"))
            .FirstOrDefault(File.Exists);
        if (path != null)
        {
            try
            {
                foreach (var row in JArray.Parse(File.ReadAllText(path)))
                {
                    var floorId = row["FloorID"]?.Value<int>() ?? 0;
                    var groupId = row["GroupID"]?.Value<int>() ?? 0;
                    var instId = row["ConfigID"]?.Value<int>() ?? 0;
                    if (floorId > 0 && groupId > 0 && instId > 0)
                        index.Add((floorId, groupId, instId));
                }
            }
            catch (Exception ex)
            {
                new Logger("Scene").Warn(
                    "ChestMonster.json could not be read; collectible monsters use the daily reset.", ex);
            }
        }

        _chestMonsters = index;
        return index;
    }
}
