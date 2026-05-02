using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.MessageSectionFinish)]
public class MissionHandlerMessageSectionFinish : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        var data = player.MessageManager!.GetMessageSectionData(info.ParamInt1);
        if (data == null) return;

        if (data.Status == MessageSectionStatus.MessageSectionFinish)
            await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        // this type wont be used in quest
        await ValueTask.CompletedTask;
    }
}