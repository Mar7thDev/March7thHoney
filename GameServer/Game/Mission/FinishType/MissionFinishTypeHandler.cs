using CastoriceHoney.Server.Data.Config;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.GameServer.Game.Player;

namespace CastoriceHoney.Server.GameServer.Game.Mission.FinishType;

public abstract class MissionFinishTypeHandler
{
    public abstract ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg);

    public abstract ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest, FinishWayExcel excel,
        object? arg);
}