using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.GetItem)]
public class MissionHandlerGetItem : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (arg is Item item)
            if (item.ItemId == info.ParamInt1)
                await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        var item = player.InventoryManager!.GetItem(excel.ParamInt1);
        if (item != null)
            await player.QuestManager!.UpdateQuestProgress(quest.QuestID, item.Count);
    }
}