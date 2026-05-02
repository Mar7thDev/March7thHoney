using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

public class GridFightBuyExpCostSyncData(GridFightSrc src, GridFightBasicInfoPb info, params uint[] param) : BaseGridFightSyncData(src, 0, param)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            GridFightLevelCost = info.BuyLevelCost
        };
    }
}