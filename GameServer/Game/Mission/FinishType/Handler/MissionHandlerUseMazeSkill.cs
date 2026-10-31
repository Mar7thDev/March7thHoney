using March7thHoney.Enums.Mission;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

/// <summary>Raised for each technique cast (SceneCastSkillCostMpCsReq); a normal attack never costs MP.</summary>
[MissionFinishType(MissionFinishTypeEnum.UseMazeSkill)]
public class MissionHandlerUseMazeSkill : MissionCountEventHandler
{
    protected override MissionFinishTypeEnum FinishType => MissionFinishTypeEnum.UseMazeSkill;
}
