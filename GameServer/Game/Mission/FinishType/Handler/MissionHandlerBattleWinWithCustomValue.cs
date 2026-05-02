using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Battle;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.BattleWinWithCustomValue)]
public class MissionHandlerBattleWinWithCustomValue : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (arg is not BattleInstance instance) return;
        if (!instance.StageId.ToString().StartsWith(info.ParamInt2.ToString())) return; // check stage id
        if (instance.BattleEndStatus != BattleEndStatus.BattleEndWin) return; // check battle status
        if (instance.BattleResult == null) return; // check battle result
        if (!instance.BattleResult.Stt.CustomValues.TryGetValue(info.ParamStr1, out var dValue))
            return; // check custom value is exist
        if ((int)dValue == info.ParamInt1) await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        // this type wont be used in quest
        await ValueTask.CompletedTask;
    }
}