using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.AllAvatarPromoteCnt)]
public class MissionHandlerAllAvatarPromoteCnt : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        // this type wont be used in mission
        await ValueTask.CompletedTask;
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        var promoteCount = 0;
        foreach (var avatar in player.AvatarManager?.AvatarData.FormalAvatars ?? []) promoteCount += avatar.Promotion;
        await player.QuestManager!.UpdateQuestProgress(quest.QuestID, promoteCount);
    }
}