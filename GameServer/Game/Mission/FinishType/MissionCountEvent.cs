using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;

namespace March7thHoney.GameServer.Game.Mission.FinishType;

/// <summary>
///     Payload of a "+N per occurrence" finish type (a relic levelled, a run finished, ...). The type is carried
///     in the event so a handler never counts the heartbeat's null arg, a battle-finish BattleInstance or another
///     event type that HandleAllFinishType fans out to every handler.
/// </summary>
/// <param name="FinishType">The finish type this occurrence belongs to.</param>
/// <param name="Count">How much progress the occurrence is worth.</param>
/// <param name="Param">Optional discriminator matched against ParamIntList (e.g. the challenge type).</param>
public sealed record MissionCountEvent(MissionFinishTypeEnum FinishType, int Count = 1, int Param = 0);

/// <summary>
///     Base for finish types that advance by <see cref="MissionCountEvent" /> only. When the row lists
///     ParamIntList values, the event's Param must be one of them.
/// </summary>
public abstract class MissionCountEventHandler : MissionFinishTypeHandler
{
    protected abstract MissionFinishTypeEnum FinishType { get; }

    /// <summary>Whether ParamIntList filters on the event Param (false for NoPara rows).</summary>
    protected virtual bool MatchParamList => false;

    private bool TryMatch(object? arg, List<int>? paramList, out int count)
    {
        count = 0;
        if (arg is not MissionCountEvent e || e.FinishType != FinishType || e.Count <= 0) return false;
        if (MatchParamList && paramList is { Count: > 0 } && !paramList.Contains(e.Param)) return false;
        count = e.Count;
        return true;
    }

    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        if (!TryMatch(arg, info.ParamIntList, out var count)) return;

        var mission = player.MissionManager!;
        await mission.AddMissionProgress(info.ID, count);
        if (mission.GetMissionProgress(info.ID) >= Math.Max(info.Progress, 1))
            await mission.FinishSubMission(info.ID);
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        if (!TryMatch(arg, excel.ParamIntList, out var count)) return;
        await player.QuestManager!.AddQuestProgress(quest.QuestID, count);
    }
}
