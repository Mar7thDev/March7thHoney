using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.HeartDialDialoguePerformanceFinish)]
public class MissionHandlerHeartDialDialoguePerformanceFinish : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (arg is string str && str.StartsWith("HeartDial_"))
        {
            var dialogueId = int.Parse(str.Replace("HeartDial_", ""));
            if (info.ParamIntList?.Contains(dialogueId) == true)
            {
                await player.MissionManager!.AddMissionProgress(info.ID, 1);
                var curProgress = player.MissionManager!.GetMissionProgress(info.ID);
                if (curProgress >= info.Progress) // finish count >= progress, finish mission
                    await player.MissionManager!.FinishSubMission(info.ID);
            }
        }
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        // this type wont be used in quest
        await ValueTask.CompletedTask;
    }
}