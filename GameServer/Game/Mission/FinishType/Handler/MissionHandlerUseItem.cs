using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.UseItem)]
public class MissionHandlerUseItem : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (arg is ItemData item)
            if (info.ParamInt1 == item.ItemId)
                await player.MissionManager!.AddMissionProgress(info.ID, 1);

        if (player.MissionManager!.GetMissionProgress(info.ID) >= info.Progress)
            await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        // this type wont be used in quest
        if (arg is ItemData item)
            if (excel.ParamInt1 == item.ItemId)
                await player.QuestManager!.AddQuestProgress(quest.QuestID, 1);
    }
}