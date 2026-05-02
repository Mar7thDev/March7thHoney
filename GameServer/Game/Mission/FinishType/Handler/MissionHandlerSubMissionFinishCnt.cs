using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.SubMissionFinishCnt)]
public class MissionHandlerSubMissionFinishCnt : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        var finishCount = 0;
        foreach (var missionId in info.ParamIntList ?? [])
        {
            var status = player.MissionManager!.GetSubMissionStatus(missionId);
            if (status == MissionPhaseEnum.Finish || status == MissionPhaseEnum.Cancel) finishCount++;
        }

        if (finishCount >= info.Progress) // finish count >= progress, finish mission
        {
            await player.MissionManager!.FinishSubMission(info.ID);
        }
        else // update progress
        {
            if (player.MissionManager!.GetMissionProgress(info.ID) != finishCount)
                await player.MissionManager!.SetMissionProgress(info.ID, finishCount);
        }
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        var finishCount = 0;
        foreach (var missionId in excel.ParamIntList)
        {
            var status = player.MissionManager!.GetSubMissionStatus(missionId);
            if (status == MissionPhaseEnum.Finish || status == MissionPhaseEnum.Cancel) finishCount++;
        }

        await player.QuestManager!.UpdateQuestProgress(quest.QuestID, finishCount);
    }
}