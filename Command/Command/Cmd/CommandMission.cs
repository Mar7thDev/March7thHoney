using System.Text;
using March7thHoney.Data;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Mission;
using March7thHoney.Internationalization;

namespace March7thHoney.Command.Command.Cmd;

[CommandInfo("mission", "Game.Command.Mission.Desc", "Game.Command.Mission.Usage", ["m"])]
public class CommandMission : ICommand
{
    [CommandMethod("0 pass")]
    public async ValueTask PassRunningMission(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var mission = arg.Target!.Player!.MissionManager!;
        var count = mission.GetRunningSubMissionIdList().Count;
        foreach (var id in mission.GetRunningSubMissionIdList()) await mission.FinishSubMission(id);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Mission.AllRunningMissionsFinished", count.ToString()));
    }

    [CommandMethod("0 finish")]
    public async ValueTask FinishRunningMission(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        if (arg.BasicArgs.Count < 1)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        if (!int.TryParse(arg.BasicArgs[0], out var missionId))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Mission.InvalidMissionId"));
            return;
        }

        var mission = arg.Target!.Player!.MissionManager!;
        var finished = await mission.ForceFinishSubMission(missionId);
        if (!finished)
        {
            await arg.SendMsg($"任务 {missionId} 未完成，可能是任务ID不存在、已完成，或资源链路仍不完整。");
            return;
        }

        await arg.SendMsg(I18NManager.Translate("Game.Command.Mission.MissionFinished", missionId.ToString()));
    }

    [CommandMethod("0 running")]
    public async ValueTask ListRunningMission(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var mission = arg.Target!.Player!.MissionManager!;
        var runningMissions = mission.GetClientRunningSubMissionList();
        if (runningMissions.Count == 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Mission.NoRunningMissions"));
            return;
        }

        await arg.SendMsg(I18NManager.Translate("Game.Command.Mission.RunningMissions"));
        Dictionary<int, List<int>> missionMap = [];
        var trackingMainMissionId = mission.GetClientTrackingMainMissionId();

        //build missionMap
        foreach (var m in runningMissions)
        {
            if (!missionMap.TryGetValue(m.MainMissionID, out var value))
            {
                value = [];
                missionMap[m.MainMissionID] = value;
            }

            value.Add(m.ID);
        }

        if ((arg.BasicArgs.Count == 1 && arg.BasicArgs[0] == "-all") || trackingMainMissionId == 0)
        {
            //Show all the missions
            await ShowMissionList(mission, missionMap, arg);
        }
        else
        {
            //Only show tracking missions
            Dictionary<int, List<int>> runningMissionMap = [];
            if (!missionMap.TryGetValue(trackingMainMissionId, out var trackedMissionList))
            {
                await ShowMissionList(mission, missionMap, arg);
                return;
            }

            runningMissionMap[trackingMainMissionId] = trackedMissionList;
            await ShowMissionList(mission, runningMissionMap, arg);
        }

        await Task.CompletedTask;
    }

    public async ValueTask ShowMissionList(MissionManager mission, Dictionary<int, List<int>> missionMap,
        CommandArg arg)
    {
        var possibleStuckIds = new List<int>();
        var morePossibleStuckIds = new List<int>();

        foreach (var list in missionMap)
        {
            await arg.SendMsg($"{I18NManager.Translate("Game.Command.Mission.MainMission")} {list.Key}：");
            var sb = new StringBuilder();
            foreach (var id in list.Value)
            {
                sb.Append($"{id}、");

                if (!id.ToString().StartsWith("10")) continue;
                possibleStuckIds.Add(id);

                var info = mission.GetSubMissionInfo(id);
                if (info?.FinishType == MissionFinishTypeEnum.PropState) morePossibleStuckIds.Add(id);
            }

            sb.Remove(sb.Length - 1, 1);

            await arg.SendMsg(sb.ToString());
        }

        if (morePossibleStuckIds.Count > 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Mission.PossibleStuckMissions"));

            var sb = new StringBuilder();
            foreach (var id in morePossibleStuckIds) sb.Append($"{id}、");

            sb.Remove(sb.Length - 1, 1);

            await arg.SendMsg(sb.ToString());
        }
        else if (possibleStuckIds.Count > 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Mission.PossibleStuckMissions"));

            var sb = new StringBuilder();
            foreach (var id in possibleStuckIds) sb.Append($"{id}、");

            sb.Remove(sb.Length - 1, 1);

            await arg.SendMsg(sb.ToString());
        }
    }

    [CommandMethod("0 reaccept")]
    public async ValueTask ReAcceptMission(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        if (arg.BasicArgs.Count < 1)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        if (!int.TryParse(arg.BasicArgs[0], out var missionId))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Mission.InvalidMissionId"));
            return;
        }

        var mission = arg.Target!.Player!.MissionManager!;
        if (!mission.TryResolveMainMissionId(missionId, out var mainMissionId))
        {
            await arg.SendMsg($"任务 {missionId} 不存在，或当前无法解析到所属主任务。");
            return;
        }

        if (!mission.HasMainMissionDefinition(mainMissionId))
        {
            await arg.SendMsg($"任务 {mainMissionId} 的主任务定义资源不完整，当前无法安全重接。");
            return;
        }

        await mission.ReAcceptMainMission(mainMissionId);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Mission.MissionReAccepted", mainMissionId.ToString()));
    }

    [CommandMethod("0 finishmain")]
    public async ValueTask FinishMainMission(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        if (arg.BasicArgs.Count < 1)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        if (!int.TryParse(arg.BasicArgs[0], out var missionId))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Mission.InvalidMissionId"));
            return;
        }

        var mission = arg.Target!.Player!.MissionManager!;
        var finished = await mission.ForceFinishMainMission(missionId);
        if (!finished)
        {
            await arg.SendMsg($"主任务 {missionId} 未完成，可能是任务ID不存在、已完成，或资源链路仍不完整。");
            return;
        }

        await arg.SendMsg(I18NManager.Translate("Game.Command.Mission.MissionFinished", missionId.ToString()));
    }
}
