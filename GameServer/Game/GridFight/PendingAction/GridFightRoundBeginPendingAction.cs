using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.GridFight.PendingAction;

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