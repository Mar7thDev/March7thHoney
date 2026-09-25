using March7thHoney.Enums.Mission;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>Raised when a light cone level-up actually gains a level.</summary>
[MissionFinishType(MissionFinishTypeEnum.AnyEquipmentLevelUp)]
public class MissionHandlerAnyEquipmentLevelUp : MissionCountEventHandler
{
    protected override MissionFinishTypeEnum FinishType => MissionFinishTypeEnum.AnyEquipmentLevelUp;
}
