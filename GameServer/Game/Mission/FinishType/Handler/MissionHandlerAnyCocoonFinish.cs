using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>
///     Win any cocoon of a cocoon type: ParamIntList holds CocoonTypeEnum values (1 calyx, 2 Echo of War,
///     3 Cavern of Corrosion). Only won CocoonConfig battles count — farm-element battles and losses do not.
/// </summary>
[MissionFinishType(MissionFinishTypeEnum.AnyCocoonFinish)]
public class MissionHandlerAnyCocoonFinish : MissionFinishTypeHandler
{
    private static bool Matches(object? arg, List<int>? paramList)
    {
        if (!CocoonBattleFilter.TryGetWonCocoon(arg, out var battle)) return false;
        if (paramList is not { Count: > 0 }) return true;
        var config = CocoonBattleFilter.GetConfig(battle);
        return config != null && paramList.Contains((int)config.CocoonType);
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
