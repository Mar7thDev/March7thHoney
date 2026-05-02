using March7thHoney.GameServer.Game.GridFight.PendingAction;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

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