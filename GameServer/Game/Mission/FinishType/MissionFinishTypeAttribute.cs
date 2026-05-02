using CastoriceHoney.Server.Enums.Mission;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType;

public class MissionFinishTypeAttribute(MissionFinishTypeEnum finishType) : Attribute
{
    public MissionFinishTypeEnum FinishType { get; private set; } = finishType;
}