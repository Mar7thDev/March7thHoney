using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>Win a specific cocoon: ParamIntList holds CocoonConfig ids (e.g. sub 401012201 = [1001]).</summary>
[MissionFinishType(MissionFinishTypeEnum.CocoonFinish)]
public class MissionHandlerCocoonFinish : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (!CocoonBattleFilter.TryGetWonCocoon(arg, out var battle)) return;
        if (!CocoonBattleFilter.ListAccepts(info.ParamIntList, battle.CocoonId)) return;
        await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        if (!CocoonBattleFilter.TryGetWonCocoon(arg, out var battle)) return;
        if (!CocoonBattleFilter.ListAccepts(excel.ParamIntList, battle.CocoonId)) return;
        await player.QuestManager!.AddQuestProgress(quest.QuestID, 1);
    }
}
