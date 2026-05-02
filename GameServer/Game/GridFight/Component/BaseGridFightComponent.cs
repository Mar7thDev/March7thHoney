using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight.Component;

public abstract class BaseGridFightComponent(GridFightInstance inst)
{
    public GridFightInstance Inst { get; } = inst;
    public abstract GridFightGameInfo ToProto();
}