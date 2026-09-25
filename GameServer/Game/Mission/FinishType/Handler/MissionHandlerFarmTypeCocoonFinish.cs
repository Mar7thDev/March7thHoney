using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>Win a cocoon of a farm type: ParamIntList holds CocoonFarmTypeEnum values (golden 6/7/8, crimson 5).</summary>
[MissionFinishType(MissionFinishTypeEnum.FarmTypeCocoonFinish)]
public class MissionHandlerFarmTypeCocoonFinish : MissionFinishTypeHandler
{
    private static bool Matches(object? arg, List<int>? paramList)
    {
        if (!CocoonBattleFilter.TryGetWonCocoon(arg, out var battle)) return false;
        var config = CocoonBattleFilter.GetConfig(battle);
        return config != null && CocoonBattleFilter.ListAccepts(paramList, (int)config.FarmType);
    }

    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (Matches(arg, info.ParamIntList)) await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        if (Matches(arg, excel.ParamIntList)) await player.QuestManager!.AddQuestProgress(quest.QuestID, 1);
    }
}
