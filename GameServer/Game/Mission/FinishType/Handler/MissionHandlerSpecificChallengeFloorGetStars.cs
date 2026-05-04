using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

// 客户端解锁「漫游签证 → 异相仲裁」依赖一组前置 Quest，
// 它们的 FinishWay 是 SpecificChallengeFloorGetStars（如混沌回忆12层3星等）。
// 私服默认所有历代挑战皆满星，因此 quest 一被接受就直接推进到完成进度。
[MissionFinishType(MissionFinishTypeEnum.SpecificChallengeFloorGetStars)]
public class MissionHandlerSpecificChallengeFloorGetStars : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        await ValueTask.CompletedTask;
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        await player.QuestManager!.UpdateQuestProgress(quest.QuestID, excel.Progress);
    }
}
