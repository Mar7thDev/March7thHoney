using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Database;
using March7thHoney.Enums;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Server.Packet.Send.PlayerSync;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Mission;

public partial class MissionManager
{
    /// <summary>
    ///     A mission with no schedule row is unbound and follows its gates. A bound mission is takeable
    ///     only while its activity module's window contains now. A missing activity table fails open.
    /// </summary>
    public bool IsMainMissionScheduled(int mainId)
    {
        MainMissionScheduleExcel.EnsureLoaded();
        if (!GameData.MainMissionScheduleData.TryGetValue(mainId, out var row) || row.ActivityModuleID == 0)
            return true;

        var schedules = GameData.ActivityConfig.ScheduleData;
        if (schedules.Count == 0) return true;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var schedule in schedules)
            if (schedule.ActivityId == row.ActivityModuleID && now >= schedule.BeginTime && now <= schedule.EndTime)
                return true;
        return false;
    }

    /// <summary>
    ///     Official Prepared shape: Type Main, every TakeParam MultiSequence is already finished, and
    ///     every still-unmet take/begin entry is PlayerLevel or WorldLevel. Branch and Daily stay hidden.
    /// </summary>
    public bool IsPreparedEligible(MainMissionExcel excel)
    {
        if (excel.Type != MainMissionTypeEnum.Main) return false;

        var hasChain = false;
        foreach (var param in excel.TakeParam)
        {
            if (param.Type != MissionTakeTypeEnum.MultiSequence) continue;
            hasChain = true;
            if (!param.IsEqual(Data)) return false;
        }

        if (!hasChain) return false;
        return GateListNumericOnly(excel.TakeParam, excel.TakeOperation != OperationEnum.And) &&
               GateListNumericOnly(excel.BeginParam, excel.BeginOperation == OperationEnum.Or);
    }

    private bool GateListNumericOnly(List<MissionParam> parameters, bool orMode)
    {
        if (parameters.Count == 0) return true;
        var anySatisfied = false;
        foreach (var param in parameters)
        {
            if (param.IsEqual(Data))
            {
                anySatisfied = true;
                continue;
            }

            if (param.Type is not (MissionTakeTypeEnum.PlayerLevel or MissionTakeTypeEnum.WorldLevel))
                return false;
        }

        return !orMode || anySatisfied;
    }

    private void AcceptPrepared(MainMissionExcel excel, MissionSync sync)
    {
        Data.SetMainMissionStatus(excel.MainMissionID, MissionPhaseEnum.Prepared);
        foreach (var subId in excel.MissionInfo.StartSubMissionList)
            MissionSyncBuilder.NotePreparedSub(sync, subId);
    }

    /// <summary>
    ///     Closes sub-missions a finished main left running. Idempotent: a second login changes nothing.
    /// </summary>
    public async ValueTask NormalizeFinishedMainSubs(bool sendPacket)
    {
        if (!Enabled) return;
        var sync = new MissionSync();
        foreach (var mainId in Data.FinishedMainMissionIds.ToList())
        {
            if (!GameData.MainMissionData.TryGetValue(mainId, out var main)) continue;
            foreach (var subId in main.SubMissionIds)
            {
                if (Data.GetSubMissionStatus(subId) == MissionPhaseEnum.Finish) continue;
                Data.SetSubMissionStatus(subId, MissionPhaseEnum.Finish);
                sync.MissionList.Add(new Proto.Mission
                {
                    Id = (uint)subId,
                    Status = MissionStatus.MissionFinish,
                    Progress = 1
                });
            }
        }

        if (sync.MissionList.Count == 0) return;
        Logger.Info($"Closed stale sub-missions of finished mains for uid {Player.Uid}: {sync.MissionList.Count}");
        MarkDirty();
        if (sendPacket) await Player.SendPacket(new PacketPlayerSyncScNotify(sync));
    }

    /// <summary>
    ///     Drops an untouched running mission whose event module is dark and is not flagged IsNotDelete.
    /// </summary>
    public async ValueTask CleanupUnavailableScheduledMissions(bool sendPacket)
    {
        if (!Enabled) return;
        MainMissionScheduleExcel.EnsureLoaded();
        foreach (var mainId in Data.RunningMainMissionIds.ToList())
        {
            if (!GameData.MainMissionScheduleData.TryGetValue(mainId, out var row) ||
                row.ActivityModuleID == 0 || row.IsNotDelete || IsMainMissionScheduled(mainId))
                continue;
            if (!GameData.MainMissionData.TryGetValue(mainId, out var main)) continue;

            var touched = false;
            foreach (var subId in main.SubMissionIds)
            {
                if (Data.GetSubMissionStatus(subId) == MissionPhaseEnum.Finish) touched = true;
                if (Data.SubMissionProgressDict.TryGetValue(subId, out var progress) && progress > 0) touched = true;
                if (touched) break;
            }

            if (touched) continue;
            Logger.Info($"Removed scheduled mission {mainId} (module {row.ActivityModuleID} is dark) for uid {Player.Uid}");
            await RemoveMainMission(mainId, sendPacket);
        }
    }

    /// <summary>
    ///     Fills floor saved values a finished sub's SetFloorSavedValue action owns when the key is
    ///     missing. Existing keys are left alone.
    /// </summary>
    public void RepairMissingFloorSavedValues()
    {
        if (!Enabled || Player.SceneData == null) return;
        var changed = false;
        foreach (var subId in Data.FinishedSubMissionIds)
        {
            if (!GameData.SubMissionInfoData.TryGetValue(subId, out var sub) || sub.SubMissionInfo == null) continue;
            foreach (var action in sub.SubMissionInfo.FinishActionList ?? [])
            {
                if (action.FinishActionType != FinishActionTypeEnum.SetFloorSavedValue) continue;
                var text = action.FinishActionParaString;
                if (text.Count < 4) continue;
                if (!int.TryParse(text[1], out var floor) || floor == 0) continue;
                if (!int.TryParse(text[3], out var value)) continue;
                var key = text[2];
                if (string.IsNullOrEmpty(key)) continue;

                if (!Player.SceneData.FloorSavedData.TryGetValue(floor, out var saved))
                {
                    saved = [];
                    Player.SceneData.FloorSavedData[floor] = saved;
                }

                if (saved.ContainsKey(key)) continue;
                saved[key] = value;
                changed = true;
            }
        }

        if (changed) DatabaseHelper.MarkDirty(Player.Uid);
    }
}
