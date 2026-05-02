using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.GridFight.PendingAction;

public class GridFightEmptyPendingAction(GridFightInstance inst) : BaseGridFightPendingAction(inst)
{
    public override GridFightPendingAction ToProto()
    {
        return new GridFightPendingAction
        {
            QueuePosition = QueuePosition
        };
    }
}