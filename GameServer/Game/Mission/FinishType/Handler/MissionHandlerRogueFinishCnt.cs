using March7thHoney.Enums.Mission;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>Raised when a Simulated Universe / Divergent Universe / Unknowable Domain / Swarm run ends, won or lost.</summary>
[MissionFinishType(MissionFinishTypeEnum.RogueFinishCnt)]
public class MissionHandlerRogueFinishCnt : MissionCountEventHandler
{
    protected override MissionFinishTypeEnum FinishType => MissionFinishTypeEnum.RogueFinishCnt;
}
