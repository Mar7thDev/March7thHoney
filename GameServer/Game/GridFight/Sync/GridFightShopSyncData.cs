using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

public class GridFightShopSyncData(GridFightSrc src, GridFightShopInfoPb data, uint level, uint groupId = 0, params uint[] param) : BaseGridFightSyncData(src, groupId, param)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            ShopSyncInfo = data.ToSyncInfo(level)
        };
    }
}