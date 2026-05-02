using CastoriceHoney.Server.GameServer.Game.GridFight.PendingAction;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.GridFight.Sync;

public class GridFightPendingActionSyncData(GridFightSrc src, BaseGridFightPendingAction action, uint groupId = 0, params uint[] param) : BaseGridFightSyncData(src, groupId, param)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            PendingAction = action.ToProto()
        };
    }
}