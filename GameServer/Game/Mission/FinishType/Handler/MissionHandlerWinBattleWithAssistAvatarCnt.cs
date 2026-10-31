using March7thHoney.Enums.Mission;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>Raised for a won battle that fielded a support character.</summary>
[MissionFinishType(MissionFinishTypeEnum.WinBattleWithAssistAvatarCnt)]
public class MissionHandlerWinBattleWithAssistAvatarCnt : MissionCountEventHandler
{
    protected override MissionFinishTypeEnum FinishType => MissionFinishTypeEnum.WinBattleWithAssistAvatarCnt;
}
