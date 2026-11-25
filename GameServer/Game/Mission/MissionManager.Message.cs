using March7thHoney.Data;
using March7thHoney.Data.Config.Task;
using March7thHoney.Enums.Mission;

namespace March7thHoney.GameServer.Game.Mission;

public partial class MissionManager
{
    /// <summary>
    ///     Starts the phone-message sections that a sub-mission's own performance act (PerformanceE with the sub's ID,
    ///     Act&lt;sub&gt;.json) opens with PlayMessage. Some chats are opened only by an act that a talk triggers on the
    ///     client: sub 102030121 ("send a message to Dan Heng") arms raid 20213 through OpenRaid, and section 1200000
    ///     lives in Act102030121's PlayMessage. A section the client opens but the server never started has nothing to
    ///     bind the replies to, so the chat renders with dead options.
    /// </summary>
    /// <returns>How many sections were started.</returns>
    public async ValueTask<int> StartActMessageSections(int subMissionId, bool sendPacket)
    {
        if (Player.MessageManager == null) return 0;
        var started = 0;
        foreach (var sectionId in ActMessageSectionIds(subMissionId))
            if (await Player.MessageManager.AddMessageSection(sectionId, sendPacket))
                started++;
        return started;
    }

    /// <summary>
    ///     Login reconcile: starts the message sections the running missions own but the save does not have - the
    ///     sections linked to a running main (MainMissionLink), the section a running MessageSectionFinish or
    ///     MessagePerformSectionFinish sub waits for (ParamInt1), and the sections a running sub's act opens. A sub
    ///     accepted by an older build, or a chat dropped by /message reset, is otherwise never reopened: these starts
    ///     only happen when a mission is accepted. Sections the player already has are left as they are, and the
    ///     login message-group response carries the started ones, so no sync is sent.
    /// </summary>
    public async ValueTask StartRunningMessageSections()
    {
        if (!Enabled || Player.MessageManager == null) return;

        var sections = new List<int>();
        var runningMains = Data.RunningMainMissionIds.ToHashSet();
        sections.AddRange(GameData.MessageSectionConfigData.Values
            .Where(section => section.MainMissionLink != 0 && runningMains.Contains(section.MainMissionLink))
            .Select(section => section.ID));

        foreach (var subId in Data.RunningSubMissionIds.ToList())
        {
            if (GameData.SubMissionInfoData.TryGetValue(subId, out var sub) &&
                sub.SubMissionInfo is { ParamInt1: > 0 } info &&
                info.FinishType is MissionFinishTypeEnum.MessagePerformSectionFinish
                    or MissionFinishTypeEnum.MessageSectionFinish)
                sections.Add(info.ParamInt1);
            sections.AddRange(ActMessageSectionIds(subId));
        }

        var started = new List<int>();
        foreach (var sectionId in sections.Distinct())
            if (await Player.MessageManager.AddMessageSection(sectionId, false))
                started.Add(sectionId);

        if (started.Count > 0)
            Logger.Debug($"Started message sections [{string.Join(", ", started)}] for the running missions of uid " +
                         $"{Player.Uid}");
    }

    private static IEnumerable<int> ActMessageSectionIds(int subMissionId)
    {
        if (!GameData.PerformanceEData.TryGetValue(subMissionId, out var performance) ||
            performance.ActInfo is not { } act)
            yield break;

        foreach (var task in act.OnInitSequece.SelectMany(sequence => sequence.TaskList)
                     .Concat(act.OnStartSequece.SelectMany(sequence => sequence.TaskList)))
            if (task is PlayMessage { MessageSectionID: > 0 } message)
                yield return message.MessageSectionID;
    }
}
