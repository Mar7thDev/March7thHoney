using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.GridFight.PendingAction;

public abstract class BaseGridFightPendingAction(GridFightInstance inst)
{
    public GridFightInstance Inst { get; set; } = inst;
    public uint QueuePosition { get; set; }
    public abstract GridFightPendingAction ToProto();
}