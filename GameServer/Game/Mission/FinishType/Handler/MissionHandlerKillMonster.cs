using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene.Entity;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.KillMonster)]
public class MissionHandlerKillMonster : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (arg is not EntityMonster { IsAlive: false } monster || monster.IsGmSpawned) return;
        // Instance IDs are local to a group. Joined fights can finish one gate and accept the next
        // before the remaining monsters are processed, so compare the entire configured target.
        if (monster.InstId != info.ParamInt2 || info.ParamInt1 > 0 && monster.GroupId != info.ParamInt1 ||
            info.LevelFloorID > 0 && monster.Scene.FloorId != info.LevelFloorID) return;
        await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        // this type wont be used in quest
        await ValueTask.CompletedTask;
    }
}
