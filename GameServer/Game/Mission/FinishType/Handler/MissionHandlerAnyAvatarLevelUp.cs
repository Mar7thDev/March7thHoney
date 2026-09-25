using March7thHoney.Enums.Mission;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>Raised when a character level-up actually gains a level.</summary>
[MissionFinishType(MissionFinishTypeEnum.AnyAvatarLevelUp)]
public class MissionHandlerAnyAvatarLevelUp : MissionCountEventHandler
{
    protected override MissionFinishTypeEnum FinishType => MissionFinishTypeEnum.AnyAvatarLevelUp;
}
