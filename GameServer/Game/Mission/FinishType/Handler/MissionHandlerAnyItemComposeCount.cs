using March7thHoney.Enums.Mission;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>Raised per synthesis (item or relic) request.</summary>
[MissionFinishType(MissionFinishTypeEnum.AnyItemComposeCount)]
public class MissionHandlerAnyItemComposeCount : MissionCountEventHandler
{
    protected override MissionFinishTypeEnum FinishType => MissionFinishTypeEnum.AnyItemComposeCount;
}
