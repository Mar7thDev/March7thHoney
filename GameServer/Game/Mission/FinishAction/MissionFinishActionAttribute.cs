using CastoriceHoney.Server.Enums.Mission;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishAction;

[AttributeUsage(AttributeTargets.Class)]
public class MissionFinishActionAttribute(FinishActionTypeEnum finishAction) : Attribute
{
    public FinishActionTypeEnum FinishAction { get; } = finishAction;
}