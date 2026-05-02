using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.BuyShopGoods)]
public class MissionHandlerBuyShopGoods : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (arg is ShopGoodsConfigExcel x)
            await player.MissionManager!.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        if (arg is ShopGoodsConfigExcel x)
            if (excel.ParamIntList.Contains(x.GoodsID))
                await player.QuestManager!.AddQuestProgress(quest.QuestID, 1);
    }
}