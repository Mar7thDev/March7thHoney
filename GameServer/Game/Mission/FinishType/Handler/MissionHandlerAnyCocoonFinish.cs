using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Battle;
using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.AnyCocoonFinish)]
public class MissionHandlerAnyCocoonFinish : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (arg is BattleInstance battle)
            if (battle.CocoonWave > 0)
                await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        if (arg is BattleInstance battle)
            if (battle.CocoonWave > 0)
                await player.QuestManager!.AddQuestProgress(quest.QuestID, 1);
    }
}