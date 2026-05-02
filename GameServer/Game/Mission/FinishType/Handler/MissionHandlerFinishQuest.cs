using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.FinishQuest)]
public class MissionHandlerFinishQuest : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        // this type wont be used in mission
        await ValueTask.CompletedTask;
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        var questCount = 0;
        foreach (var qid in excel.ParamIntList)
        {
            var status = player.QuestManager?.GetQuestStatus(qid);
            if (status == QuestStatus.QuestFinish || status == QuestStatus.QuestClose)
                questCount++;
        }

        await player.QuestManager!.UpdateQuestProgress(quest.QuestID, questCount);
    }
}