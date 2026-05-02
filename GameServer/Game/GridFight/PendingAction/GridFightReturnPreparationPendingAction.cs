using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight.PendingAction;

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