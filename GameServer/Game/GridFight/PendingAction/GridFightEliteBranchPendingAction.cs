using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.GridFight.PendingAction;

public class GridFightEliteBranchPendingAction(GridFightInstance inst) : BaseGridFightPendingAction(inst)
{
    public override GridFightPendingAction ToProto()
    {
        return new GridFightPendingAction
        {
            EliteBranchAction = new GridFightEliteBranchActionInfo(),
            QueuePosition = QueuePosition
        };
    }
}