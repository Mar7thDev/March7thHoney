using March7thHoney.Enums.Mission;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>Raised when overworld destructibles are hit; Count is the number destroyed.</summary>
[MissionFinishType(MissionFinishTypeEnum.PropDestruct)]
public class MissionHandlerPropDestruct : MissionCountEventHandler
{
    protected override MissionFinishTypeEnum FinishType => MissionFinishTypeEnum.PropDestruct;
}
