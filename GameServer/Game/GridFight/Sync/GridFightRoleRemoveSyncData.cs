using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

public class GridFightRoleRemoveSyncData(GridFightSrc src, GridFightRoleInfoPb role, uint groupId = 0, params uint[] param) : BaseGridFightSyncData(src, groupId, param)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            RemoveRoleUniqueId = role.UniqueId
        };
    }
}