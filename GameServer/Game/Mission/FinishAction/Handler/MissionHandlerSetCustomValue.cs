using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;

namespace March7thHoney.GameServer.Game.Mission.FinishAction.Handler;

[MissionFinishAction(FinishActionTypeEnum.SetCustomValue)]
public class MissionHandlerSetCustomValue : MissionFinishActionHandler
{
    public override async ValueTask OnHandle(List<int> @params, List<string> paramString, PlayerInstance player)
    {
        if (player.MissionManager == null) return;

        int mainMissionId;
        string key;
        uint value;

        if (paramString.Count >= 3 && int.TryParse(paramString[0], out mainMissionId) &&
            uint.TryParse(paramString[2], out value))
        {
            key = paramString[1];
        }
        else if (paramString.Count >= 2 && uint.TryParse(paramString[1], out value))
        {
            key = paramString[0];
            mainMissionId = player.MissionManager.CurrentFinishActionMainMissionId;
            if (mainMissionId <= 0)
                mainMissionId = @params.Count > 0 ? @params[0] : player.MissionManager.Data.TrackingMainMissionId;
        }
        else
        {
            return;
        }

        if (mainMissionId <= 0 || string.IsNullOrEmpty(key)) return;
        await player.MissionManager.UpdateMainMissionCustomValue(mainMissionId, value, key);
    }
}
