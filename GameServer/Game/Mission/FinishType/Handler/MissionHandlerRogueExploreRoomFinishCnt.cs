using March7thHoney.Enums.Mission;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>Raised when a Simulated Universe room is cleared.</summary>
[MissionFinishType(MissionFinishTypeEnum.RogueExploreRoomFinishCnt)]
public class MissionHandlerRogueExploreRoomFinishCnt : MissionCountEventHandler
{
    protected override MissionFinishTypeEnum FinishType => MissionFinishTypeEnum.RogueExploreRoomFinishCnt;
}
