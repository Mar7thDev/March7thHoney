using March7thHoney.Enums.Mission;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>
///     Raised when a GridFight run settles. ParamIntList filters on the run mode: 0 tutorial, 1 standard,
///     2 overclock (quest 7302115, the overclock unlock named by GridFight_OC_UnlockQuest, accepts 0 and 1).
/// </summary>
[MissionFinishType(MissionFinishTypeEnum.GridFightFinishGamePlay)]
public class MissionHandlerGridFightFinishGamePlay : MissionCountEventHandler
{
    protected override MissionFinishTypeEnum FinishType => MissionFinishTypeEnum.GridFightFinishGamePlay;

    protected override bool MatchParamList => true;
}
