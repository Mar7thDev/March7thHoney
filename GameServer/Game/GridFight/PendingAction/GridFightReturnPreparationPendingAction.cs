using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.GridFight.PendingAction;

public class GridFightReturnPreparationPendingAction(GridFightInstance inst) : BaseGridFightPendingAction(inst)
{
    public override GridFightPendingAction ToProto()
    {
        return new GridFightPendingAction
        {
            ReturnPreparationAction = new GridFightReturnPreparationActionInfo(),
            QueuePosition = QueuePosition
        };
    }
}