using March7thHoney.Enums.Mission;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>Raised when relics are salvaged; Count is the number of relics.</summary>
[MissionFinishType(MissionFinishTypeEnum.ResolveRelic)]
public class MissionHandlerResolveRelic : MissionCountEventHandler
{
    protected override MissionFinishTypeEnum FinishType => MissionFinishTypeEnum.ResolveRelic;
}
