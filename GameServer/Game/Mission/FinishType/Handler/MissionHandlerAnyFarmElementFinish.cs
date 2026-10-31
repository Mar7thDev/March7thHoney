using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.AnyFarmElementFinish)]
public class MissionHandlerAnyFarmElementFinish : MissionFinishTypeHandler
{
    // Cocoon battles set MappingInfoId too, so only a won non-cocoon battle is a Stagnant Shadow clear.
    private static bool IsFarmElementWin(object? arg) =>
        arg is BattleInstance { MappingInfoId: > 0, CocoonId: 0 } battle &&
        battle.BattleResult?.EndStatus == BattleEndStatus.BattleEndWin;

    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (IsFarmElementWin(arg))
            await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        if (IsFarmElementWin(arg))
            await player.QuestManager!.AddQuestProgress(quest.QuestID, 1);
    }
}
