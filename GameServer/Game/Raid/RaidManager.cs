using March7thHoney.Data;
using March7thHoney.Database;
using March7thHoney.Database.Scene;
using March7thHoney.Enums.Mission;
using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.Lineup;
using March7thHoney.GameServer.Server.Packet.Send.Raid;
using March7thHoney.Proto;
using March7thHoney.Util;
using RaidData = March7thHoney.Database.Scene.RaidData;

namespace March7thHoney.GameServer.Game.Raid;

public class RaidManager(PlayerInstance player) : BasePlayerManager<RaidData>(player)
{
    private static readonly Logger Logger = new("Raid");

    #region Information

    public RaidStatus GetRaidStatus(int raidId, int worldLevel = 0)
    {
        if (!Data.RaidRecordDatas.TryGetValue(raidId, out var dict)) return RaidStatus.None;
        if (!dict.TryGetValue(worldLevel, out var record)) return RaidStatus.None;
        return record.Status;
    }

    #endregion

    #region Player Handler

    public async ValueTask OnLogin()
    {
        // try resume
        var record = GetCurrentRecord();
        if (record != null && !GameData.RaidConfigData.ContainsKey(record.RaidId * 100 + record.WorldLevel))
        {
            // The raid no longer exists in this version's tables, so LeaveRaid could never release the player:
            // abandon the run and return to where it was entered from instead of resuming a dead session.
            Logger.Warn($"Player {Player.Uid} logged in inside raid {record.RaidId} (world level {record.WorldLevel}) " +
                        "that has no RaidConfig; abandoning the run.");
            await AbandonCurrentRun(true);
            record = null;
        }

        if (record != null)
        {
            await Player.SendPacket(new PacketRaidInfoNotify(record));
        }
        else
        {
            var orphaned = Data.CurRaidId != 0;
            Data.CurRaidId = 0;
            Data.CurRaidWorldLevel = 0;
            if (orphaned) MarkDirty();
            // Login loads the saved scene directly. A missing run must not leave a stale raid location
            // to be loaded again; active story lines own their location and are handled separately.
            if (Player.StoryLineManager?.StoryLineData.CurStoryLineId is not > 0 &&
                GameData.MazePlaneData.GetValueOrDefault(Player.Data.PlaneId)?.PlaneType == PlaneTypeEnum.Raid)
            {
                await ReleaseRaidLineup();
                Player.TryPlaceAtEntrance(PlayerInstance.DefaultSpawnEntryId);
            }
            await Player.SendPacket(new PacketRaidInfoNotify());
        }
    }

    /// <summary>
    ///     Login-time abandon for <c>ServerOption.ResetLocationOnLogin</c>: clears the open raid run without loading a
    ///     scene. The caller relocates the player.
    /// </summary>
    public async ValueTask AbandonAtLogin()
    {
        if (Data.CurRaidId == 0) return;
        await AbandonCurrentRun(false);
    }

    private async ValueTask AbandonCurrentRun(bool placeAtReturnPoint)
    {
        var record = GetCurrentRecord();
        Data.CurRaidId = 0;
        Data.CurRaidWorldLevel = 0;
        MarkDirty();
        await ReleaseRaidLineup();
        if (record == null) return;

        await ClearRaid(record.RaidId, record.WorldLevel);
        if (placeAtReturnPoint &&
            !Player.TryPlaceAtEntrance(record.OldEntryId, record.OldPos, record.OldRot, PlaneTypeEnum.Raid))
            Player.TryPlaceAtEntrance(PlayerInstance.DefaultSpawnEntryId);
    }

    /// <summary>The record of the open raid run, or null when no run is open or its record is gone.</summary>
    private RaidRecord? GetCurrentRecord()
    {
        if (Data.CurRaidId == 0) return null;
        if (!Data.RaidRecordDatas.TryGetValue(Data.CurRaidId, out var dict)) return null;
        return dict.GetValueOrDefault(Data.CurRaidWorldLevel);
    }

    #endregion

    #region Player Action

    public async ValueTask<RaidRecord?> EnterRaid(int raidId, int worldLevel, List<int>? avatarList = null,
        bool enterSaved = false)
    {
        if (Data.CurRaidId != 0) return null;

        GameData.RaidConfigData.TryGetValue(raidId * 100 + worldLevel, out var excel);
        if (excel == null) return null; // not exist

        Data.RaidRecordDatas.TryGetValue(raidId, out var dict);
        dict ??= [];
        if (dict.ContainsKey(worldLevel) && !enterSaved)
            // clear old record
            await ClearRaid(raidId, worldLevel);
        dict.TryGetValue(worldLevel, out var record);

        Data.CurRaidId = excel.RaidID;
        Data.CurRaidWorldLevel = worldLevel;

        if (record == null)
        {
            // first enter
            var entranceId = 0;
            var firstMission = excel.MainMissionIDList[0];
            var subMissionId =
                GameData.MainMissionData[firstMission].MissionInfo!.StartSubMissionList[0]; // get the first sub mission
            var subMission = GameData.SubMissionInfoData[subMissionId];

            entranceId =
                int.Parse(subMission.SubMissionInfo!.LevelFloorID.ToString()
                    .Replace("00", "0")); // get entrance id  ( need to find a better way to do it )

            if (!GameData.MapEntranceData.ContainsKey(entranceId)) entranceId = subMission.SubMissionInfo!.LevelFloorID;

            if (avatarList?.Count > 0)
            {
                Player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupHeliobus, avatarList);
                await Player.SendPacket(new PacketSyncLineupNotify(Player.LineupManager!.GetCurLineup()!));
            }
            else if (excel.TeamType == RaidTeamTypeEnum.TrialOnly)
            {
                // set lineup
                List<int> list = [..excel.TrialAvatarList];
                if (list.Count > 0)
                {
                    if (Player.Data.CurrentGender == Gender.Man)
                    {
                        foreach (var avatar in excel.TrialAvatarList)
                            if (avatar > 10000) // else is Base Avatar
                                if (avatar.ToString().EndsWith("8002") ||
                                    avatar.ToString().EndsWith("8004") ||
                                    avatar.ToString().EndsWith("8006"))
                                    list.Remove(avatar);
                    }
                    else
                    {
                        foreach (var avatar in excel.TrialAvatarList)
                            if (avatar > 10000) // else is Base Avatar
                                if (avatar.ToString().EndsWith("8001") ||
                                    avatar.ToString().EndsWith("8003") ||
                                    avatar.ToString().EndsWith("8005"))
                                    list.Remove(avatar);
                    }
                }

                Player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupHeliobus, list);
                if (excel.LockCaptain) Player.LineupManager!.GetCurLineup()!.LeaderAvatarId = excel.LockCaptainAvatarID;
                await Player.SendPacket(new PacketSyncLineupNotify(Player.LineupManager!.GetCurLineup()!));
            }
            else
            {
                // set cur lineup
                var lineup = Player.LineupManager!.GetCurLineup()!;
                Player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupHeliobus,
                    lineup.BaseAvatars!.Select(x => x.SpecialAvatarId > 0 ? x.SpecialAvatarId / 10 : x.BaseAvatarId)
                        .ToList());
                await Player.SendPacket(new PacketSyncLineupNotify(Player.LineupManager!.GetCurLineup()!));
            }

            var oldEntryId = Player.Data.EntryId;
            var oldPos = Player.Data.Pos;
            var oldRot = Player.Data.Rot;

            await Player.MissionManager!.AcceptMainMission(firstMission);

            await Player.EnterScene(entranceId, 0, false);

            record = new RaidRecord
            {
                PlaneId = Player.Data.PlaneId,
                FloorId = Player.Data.FloorId,
                EntryId = entranceId,
                Pos = Player.Data.Pos!,
                Rot = Player.Data.Rot!,
                Status = RaidStatus.Doing,
                WorldLevel = worldLevel,
                RaidId = raidId,
                Lineup = Player.LineupManager!.GetCurLineup()!.BaseAvatars!,
                OldEntryId = oldEntryId,
                OldPos = oldPos!,
                OldRot = oldRot!
            };

            if (Data.RaidRecordDatas.TryGetValue(raidId, out var value))
                value[worldLevel] = record;
            else
                Data.RaidRecordDatas[raidId] = new Dictionary<int, RaidRecord> { { worldLevel, record } };
        }
        else
        {
            // just resume
            record.Status = RaidStatus.Doing;
            Player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupHeliobus,
                record.Lineup.Select(x => x.SpecialAvatarId > 0 ? x.SpecialAvatarId : x.BaseAvatarId).ToList());
            await Player.LoadScene(record.PlaneId, record.FloorId, record.EntryId, record.Pos, record.Rot, false);
        }

        await Player.SendPacket(new PacketRaidInfoNotify(record));
        return record;
    }

    public async ValueTask CheckIfLeaveRaid()
    {
        var record = GetCurrentRecord();
        if (record == null) return;

        GameData.RaidConfigData.TryGetValue(record.RaidId * 100 + record.WorldLevel, out var excel);
        if (excel == null) return;
        var leave = true;
        foreach (var id in excel.MainMissionIDList)
            if (Player.MissionManager!.GetMainMissionStatus(id) != MissionPhaseEnum.Finish)
                leave = false;

        if (leave)
        {
            await FinishRaid();
            // finish
            await Player.MissionManager!.HandleFinishType(MissionFinishTypeEnum.RaidFinishCnt);
        }
    }

    public async ValueTask FinishRaid()
    {
        var record = GetCurrentRecord();
        if (record == null) return;

        GameData.RaidConfigData.TryGetValue(record.RaidId * 100 + record.WorldLevel, out var config);
        if (config == null) return;

        record.Status = RaidStatus.Finish;

        await Player.SendPacket(new PacketRaidInfoNotify(record));
    }

    public async ValueTask LeaveRaid(bool save)
    {
        if (Data.CurRaidId == 0) return;

        var record = GetCurrentRecord();
        if (record == null)
        {
            // The open run lost its record, so nothing says where to return: release the player to the default
            // spawn instead of keeping them on a raid floor whose exit can never succeed.
            Data.CurRaidId = 0;
            Data.CurRaidWorldLevel = 0;
            await ReleaseRaidLineup();
            MarkDirty();
            await Player.SendPacket(new PacketRaidInfoNotify());
            await Player.EnterScene(PlayerInstance.DefaultSpawnEntryId, 0, true);
            return;
        }

        // A raid missing from this version's tables is left like an unfinished one instead of trapping the player.
        GameData.RaidConfigData.TryGetValue(record.RaidId * 100 + record.WorldLevel, out var config);

        record.PlaneId = Player.Data.PlaneId;
        record.FloorId = Player.Data.FloorId;
        record.EntryId = Player.Data.EntryId;
        record.Pos = Player.Data.Pos!;
        record.Rot = Player.Data.Rot!;

        await ReleaseRaidLineup();

        if (record.Status == RaidStatus.Finish)
        {
            await Player.SendPacket(new PacketRaidInfoNotify());
            if (config?.FinishEntranceID is not > 0 ||
                !(await Player.TryEnterScene(config.FinishEntranceID, 0, true)).Success)
                await ReturnToOldSpot(record);
        }
        else
        {
            await ReturnToOldSpot(record);

            // reset raid info

            await Player.SendPacket(new PacketRaidInfoNotify());

            if (!save || config == null) await ClearRaid(record.RaidId, record.WorldLevel);
        }

        if (config == null && record.Status == RaidStatus.Finish) await ClearRaid(record.RaidId, record.WorldLevel);
        Data.CurRaidId = 0;
        Data.CurRaidWorldLevel = 0;
        MarkDirty();
    }

    private async ValueTask ReleaseRaidLineup()
    {
        if (Player.LineupManager!.GetCurLineup()?.IsExtraLineup() == true)
        {
            Player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupNone, []);
            await Player.SendPacket(new PacketSyncLineupNotify(Player.LineupManager!.GetCurLineup()!));
        }
    }

    private async ValueTask ReturnToOldSpot(RaidRecord record)
    {
        // A record without a usable return entrance (legacy save, entrance gone) falls back to the default spawn.
        if (GameData.MapEntranceData.TryGetValue(record.OldEntryId, out var entrance) &&
            GameData.MazePlaneData.GetValueOrDefault(entrance.PlaneID)?.PlaneType != PlaneTypeEnum.Raid &&
            (await Player.TryEnterScene(record.OldEntryId, 0, true)).Success)
            await Player.MoveTo(record.OldPos, record.OldRot);
        else
            await Player.EnterScene(PlayerInstance.DefaultSpawnEntryId, 0, true);
    }

    public async ValueTask ClearRaid(int raidId, int worldLevel)
    {
        if (!Data.RaidRecordDatas.TryGetValue(raidId, out var dict)) return;
        if (!dict.TryGetValue(worldLevel, out var record)) return;

        // A raid missing from this version's tables still drops its record; only its missions are unknown.
        GameData.RaidConfigData.TryGetValue(raidId * 100 + worldLevel, out var config);

        HashSet<int> floorIds = [];
        foreach (var missionId in config?.MainMissionIDList ?? [])
        {
            await Player.MissionManager!.RemoveMainMission(missionId);

            GameData.MainMissionData.TryGetValue(missionId, out var mission);
            if (mission?.MissionInfo.SubMissionList == null) continue;
            foreach (var i in mission.MissionInfo.SubMissionList.Select(x => x.LevelFloorID).ToHashSet())
                floorIds.Add(i);
        }

        dict.Remove(worldLevel);

        if (dict.Count == 0) Data.RaidRecordDatas.Remove(raidId);
        MarkDirty();

        // reset scene data
        foreach (var floorId in floorIds)
        {
            Player.SceneData!.PropTimelineData.Remove(floorId);
            Player.SceneData!.GroupPropertyData.Remove(floorId);
            Player.SceneData!.FloorTargetPuzzleGroupData.Remove(floorId);
            Player.SceneData!.FloorSavedData.Remove(floorId);
            Player.SceneData!.ScenePropData.Remove(floorId);
        }

        await Player.SendPacket(new PacketDelSaveRaidScNotify(raidId, worldLevel));
    }

    #endregion
}
