using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishAction.Handler;

[MissionFinishAction(FinishActionTypeEnum.delSubMission)]
public class MissionHandlerDelSubMission : MissionFinishActionHandler
{
    public override async ValueTask OnHandle(List<int> @params, List<string> paramString, PlayerInstance player)
    {
        if (@params.Count < 1) return;

        foreach (var subMissionId in @params)
        {
            await player.MissionManager!.AcceptSubMission(subMissionId);
            await player.MissionManager!.FinishSubMission(subMissionId);
        }
    }
}