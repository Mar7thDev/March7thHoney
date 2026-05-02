using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game.Task;

public class MissionTaskTrigger(PlayerInstance player)
{
    public PlayerInstance Player { get; } = player;

    public void TriggerMissionTask(int missionId)
    {
        GameData.SubMissionInfoData.TryGetValue(missionId, out var subMission);
        if (subMission != null)
            TriggerMissionTask(subMission.SubMissionTaskInfo ?? new LevelGraphConfigInfo(), subMission);
    }

    public void TriggerMissionTask(LevelGraphConfigInfo subMissionTaskInfo, SubMissionData subMission)
    {
        foreach (var task in subMissionTaskInfo.OnInitSequece)
            Player.TaskManager?.LevelTask.TriggerInitAct(task, subMission);

        foreach (var task in subMissionTaskInfo.OnStartSequece)
            Player.TaskManager?.LevelTask.TriggerStartAct(task, subMission);
    }
}