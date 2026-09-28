using March7thHoney.Enums.Mission;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>Raised when a Forgotten Hall (1) / Pure Fiction (2) / Apocalyptic Shadow (3) challenge is won; Param is that challenge type and ParamIntList lists the accepted types.</summary>
[MissionFinishType(MissionFinishTypeEnum.ChallengeFinishCnt)]
public class MissionHandlerChallengeFinishCnt : MissionCountEventHandler
{
    protected override MissionFinishTypeEnum FinishType => MissionFinishTypeEnum.ChallengeFinishCnt;

    protected override bool MatchParamList => true;
}
