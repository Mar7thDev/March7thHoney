using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.RaidFinishCnt)]
public class MissionHandlerRaidFinishCnt : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        var finishCount =
            (info.ParamIntList ?? []).Count(raidId => player.RaidManager!.GetRaidStatus(raidId) == RaidStatus.Finish);

        if (finishCount >= info.Progress) await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        // this type wont be used in quest
        var finishCount = excel.ParamIntList.Count(raidLevel =>
            player.RaidManager!.GetRaidStatus(excel.ParamInt1, raidLevel) == RaidStatus.Finish);

        await player.QuestManager!.UpdateQuestProgress(quest.QuestID, finishCount);
    }
}