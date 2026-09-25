using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Database;
using March7thHoney.Database.Activity;
using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Server.Packet.Send.Activity;
using March7thHoney.GameServer.Server.Packet.Send.Lineup;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.Activity.Activities;

public class TrialActivityInstance : BaseActivityInstance
{
    private const int TrialLineupIndex = (int)ExtraLineupType.LineupStageTrial + 10;

    public TrialActivityInstance(ActivityManager manager) : base(manager)
    {
        Data = ActivityManager.Data.TrialActivityData;
    }

    public TrialActivityData Data { get; set; }

    /// <summary>Resolves a Role Trial stage; null for unknown ids and for non-trial demo rows (SpecialDisplay).</summary>
    public static AvatarDemoConfigExcel? GetTrialStage(int stageId)
    {
        return GameData.AvatarDemoConfigData.TryGetValue(stageId, out var stage) && stage.IsTrialActivity
            ? stage
            : null;
    }

    public async ValueTask<Retcode> StartActivity(int stageId)
    {
        var player = ActivityManager.Player;
        var excel = GetTrialStage(stageId);
        if (excel == null || excel.MapEntranceID == 0 || excel.TrialAvatarList.Length == 0)
            return Retcode.RetReqParaInvalid;

        var lineupManager = player.LineupManager!;
        var lineupData = lineupManager.Data;

        // Everything needed to undo the start if the trial scene cannot be entered.
        var previousStageId = Data.CurTrialStageId;
        var previousInstance = ActivityManager.TrialActivityInstance;
        var previousReturn = (Data.ReturnEntryId, Data.ReturnPos, Data.ReturnRot);
        lineupData.Lineups.TryGetValue(TrialLineupIndex, out var previousLineup);
        var previousExtraLineup = lineupData.CurExtraLineup;

        await lineupManager.DestroyExtraLineup(ExtraLineupType.LineupStageTrial);

        // Starting a trial from inside another trial keeps the original world return point.
        if (previousStageId == 0)
        {
            Data.ReturnEntryId = player.Data.EntryId;
            Data.ReturnPos = player.Data.Pos!;
            Data.ReturnRot = player.Data.Rot!;
        }

        Data.CurTrialStageId = stageId;
        ActivityManager.TrialActivityInstance = this;
        lineupManager.SetExtraLineup(ExtraLineupType.LineupStageTrial, excel.TrialAvatarList.ToList(), true);

        // SPList is parallel to TrialAvatarList (the official trial team starts with these SP values).
        for (var i = 0; i < excel.TrialAvatarList.Length && i < excel.SPList.Length; i++)
            player.AvatarManager!.GetTrialAvatar(excel.TrialAvatarList[i])?.SetCurSp(excel.SPList[i], true);

        if ((await player.TryEnterScene(excel.MapEntranceID, 0, true)).Success)
        {
            DatabaseHelper.MarkDirty(player.Uid);
            return Retcode.RetSucc;
        }

        Data.CurTrialStageId = previousStageId;
        (Data.ReturnEntryId, Data.ReturnPos, Data.ReturnRot) = previousReturn;
        ActivityManager.TrialActivityInstance = previousStageId != 0 ? previousInstance : null;
        if (previousLineup != null) lineupData.Lineups[TrialLineupIndex] = previousLineup;
        else lineupData.Lineups.Remove(TrialLineupIndex);
        lineupData.CurExtraLineup = previousExtraLineup;
        var curLineup = lineupManager.GetCurLineup();
        if (curLineup != null) await player.SendPacket(new PacketSyncLineupNotify(curLineup));
        return Retcode.RetFail;
    }

    /// <summary>Leaves the running trial without recording a clear and returns the player to the start point.</summary>
    public async ValueTask LeaveActivity()
    {
        var player = ActivityManager.Player;
        var (returnEntryId, returnPos, returnRot) = ClearRunningStage();

        await player.LineupManager!.DestroyExtraLineup(ExtraLineupType.LineupStageTrial);
        player.LineupManager.SetExtraLineup(ExtraLineupType.LineupNone, []);

        await ReturnToOrigin(returnEntryId, returnPos, returnRot);
        DatabaseHelper.MarkDirty(player.Uid);
    }

    /// <summary>
    ///     Records a cleared trial (once) and sends, in the official order, TrialActivityDataChangeScNotify, the
    ///     return scene and CurTrialActivityScNotify(Finish). Called before PVEBattleResultScRsp.
    /// </summary>
    public async ValueTask FinishActivity()
    {
        var player = ActivityManager.Player;
        var stageId = Data.CurTrialStageId;
        if (stageId == 0) return;
        var (returnEntryId, returnPos, returnRot) = ClearRunningStage();

        var record = Data.Activities.Find(x => x.StageId == stageId);
        if (record == null)
        {
            record = new TrialActivityResultData { StageId = stageId };
            Data.Activities.Add(record);
        }

        DatabaseHelper.MarkDirty(player.Uid);
        await player.SendPacket(new PacketTrialActivityDataChangeScNotify(record,
            ActivityManager.GetTrialAvatarProtoList()));

        // The official sends no lineup packet here; the return scene notify carries the player's own lineup.
        var lineupData = player.LineupManager!.Data;
        lineupData.Lineups.Remove(TrialLineupIndex);
        if (lineupData.CurExtraLineup == TrialLineupIndex) lineupData.CurExtraLineup = -1;

        await ReturnToOrigin(returnEntryId, returnPos, returnRot);
        await player.SendPacket(new PacketCurTrialActivityScNotify((uint)stageId, TrialActivityStatus.Finish));
    }

    private (int EntryId, Position Pos, Position Rot) ClearRunningStage()
    {
        var origin = (Data.ReturnEntryId, Data.ReturnPos, Data.ReturnRot);
        Data.CurTrialStageId = 0;
        Data.ReturnEntryId = 0;
        ActivityManager.TrialActivityInstance = null;
        return origin;
    }

    private async ValueTask ReturnToOrigin(int entryId, Position pos, Position rot)
    {
        var player = ActivityManager.Player;
        if (entryId != 0 && GameData.MapEntranceData.TryGetValue(entryId, out var entrance) &&
            GameData.MazePlaneData.TryGetValue(entrance.PlaneID, out var plane) &&
            plane.PlaneType != PlaneTypeEnum.TrialActivity)
        {
            await player.LoadScene(entrance.PlaneID, entrance.FloorID, entryId, pos, rot, true);
            if (player.SceneInstance?.EntryId == entryId) return;
        }

        await player.EnterScene(ActivityManager.TrialFallbackEntryId, 0, true);
    }
}
