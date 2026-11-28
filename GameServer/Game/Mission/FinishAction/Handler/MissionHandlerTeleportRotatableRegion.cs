using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;

namespace March7thHoney.GameServer.Game.Mission.FinishAction.Handler;

[MissionFinishAction(FinishActionTypeEnum.TeleportRotatableRegion)]
public class MissionHandlerTeleportRotatableRegion : MissionFinishActionHandler
{
    public override async ValueTask OnHandle(List<int> @params, List<string> paramString, PlayerInstance player)
    {
        if (@params.Count < 1) return;

        var entryId = @params[0];
        var anchorGroup = @params.Count > 1 ? @params[1] : 0;
        var anchorId = @params.Count > 2 ? @params[2] : 0;
        await player.EnterSceneByEntranceId(entryId, anchorGroup, anchorId, true);
    }
}
