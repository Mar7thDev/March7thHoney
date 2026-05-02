using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.TimeLineSetStateCnt)]
public class MissionHandlerTimeLineSetStateCnt : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        // TODO
        await ValueTask.CompletedTask;
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        // TODO
        await ValueTask.CompletedTask;
    }
}