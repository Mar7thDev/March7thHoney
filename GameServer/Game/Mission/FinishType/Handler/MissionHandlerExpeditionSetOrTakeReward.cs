using March7thHoney.Enums.Mission;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>Raised when assignments are dispatched or their rewards are claimed.</summary>
[MissionFinishType(MissionFinishTypeEnum.ExpeditionSetOrTakeReward)]
public class MissionHandlerExpeditionSetOrTakeReward : MissionCountEventHandler
{
    protected override MissionFinishTypeEnum FinishType => MissionFinishTypeEnum.ExpeditionSetOrTakeReward;
}
