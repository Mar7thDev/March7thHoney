using March7thHoney.Enums.Mission;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>Raised when a relic enhancement actually gains a level.</summary>
[MissionFinishType(MissionFinishTypeEnum.AnyRelicLevelUp)]
public class MissionHandlerAnyRelicLevelUp : MissionCountEventHandler
{
    protected override MissionFinishTypeEnum FinishType => MissionFinishTypeEnum.AnyRelicLevelUp;
}
