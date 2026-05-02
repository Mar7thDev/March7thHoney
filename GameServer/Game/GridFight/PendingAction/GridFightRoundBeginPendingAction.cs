using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight.PendingAction;

public class GridFightRoundBeginPendingAction(GridFightInstance inst) : BaseGridFightPendingAction(inst)
{
    public override GridFightPendingAction ToProto()
    {
        return new GridFightPendingAction
        {
            RoundBeginAction = new GridFightRoundBeginActionInfo(),
            QueuePosition = QueuePosition
        };
    }
}