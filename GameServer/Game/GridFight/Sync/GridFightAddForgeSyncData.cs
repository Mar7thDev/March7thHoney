using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

public class GridFightAddForgeSyncData(GridFightSrc src, GridFightForgeInfoPb info, uint groupId = 0, params uint[] syncParams) : BaseGridFightSyncData(src, groupId, syncParams)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            AddForgeInfo = info.ToProto()
        };
    }
}