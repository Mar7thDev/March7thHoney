using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.EquipmentLevelCnt)]
public class MissionHandlerEquipmentLevelCnt : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        // this type wont be used in mission
        await ValueTask.CompletedTask;
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        var equipmentCount = 0;
        foreach (var equipment in player.InventoryManager?.Data.EquipmentItems ?? [])
            if (equipment.Level >= excel.ParamInt1)
                equipmentCount++;

        await player.QuestManager!.UpdateQuestProgress(quest.QuestID, equipmentCount);
    }
}