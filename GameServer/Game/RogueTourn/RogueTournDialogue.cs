using System.Collections.Concurrent;
using March7thHoney.Data;
using March7thHoney.Data.Config.Rogue;

namespace March7thHoney.GameServer.Game.RogueTourn;

/// <summary>
///     A Divergent Universe event's Act graph read as the server needs it. The client executes the graph:
///     "triggerOpt_*" is fired client-side when an option is picked, "triggerEvent_*" only when the SERVER reports
///     that dialogue event, and the branch holding FinishLevelGraph (usually behind "ALL_TALK_END") ends the talk.
/// </summary>
public sealed class RogueTournEventDialogue
{
    private const string TalkEnd = "ALL_TALK_END";
    private const string EventPrefix = "triggerEvent_";

    private static readonly ConcurrentDictionary<int, RogueTournEventDialogue?> Cache = new();

    private readonly Dictionary<string, List<RogueTournDialogueBranch>> _byWait = [];
    private readonly List<int> _eventOrder = [];
    private readonly Dictionary<int, string> _eventStrings = [];
    private readonly Dictionary<int, string> _optionTrigger = [];

    private RogueTournEventDialogue(RogueTournDialogueGraph graph)
    {
        NpcId = graph.NpcId;
        TalkNameId = graph.TalkNameId;
        HashSet<int> seen = [];
        foreach (var branch in graph.Branches)
        {
            foreach (var option in branch.Options)
            {
                _optionTrigger[option.OptionId] = option.Trigger;
                if (seen.Add(option.OptionId)) OptionIds.Add(option.OptionId);
            }

            foreach (var dialogueEvent in branch.Events)
                if (_eventStrings.TryAdd(dialogueEvent.EventId, dialogueEvent.Success))
                    _eventOrder.Add(dialogueEvent.EventId);

            if (branch.Wait.Length > 0)
            {
                if (!_byWait.TryGetValue(branch.Wait, out var list)) _byWait[branch.Wait] = list = [];
                list.Add(branch);
            }
            else
            {
                EntryOptionIds.AddRange(branch.Options.Select(option => option.OptionId));
            }
        }

        OptionIds.Sort();
        foreach (var id in OptionIds)
        {
            var (fired, _, finished) = Closure([], _optionTrigger.GetValueOrDefault(id, ""));
            if (finished || fired.Contains(TalkEnd)) FinishingOptionIds.Add(id);
        }
    }

    public int NpcId { get; }
    public int TalkNameId { get; }

    // Every option the graph can show, ascending: the announced pool.
    public List<int> OptionIds { get; } = [];

    // Options whose client-side trigger reaches FinishLevelGraph without a server report.
    public List<int> FinishingOptionIds { get; } = [];

    public List<int> EntryOptionIds { get; } = [];

    public static RogueTournEventDialogue? For(int npcId)
    {
        return Cache.GetOrAdd(npcId, id => GameData.RogueTournDialogueData.TryGetValue(id, out var graph)
            ? new RogueTournEventDialogue(graph)
            : null);
    }

    public bool IsFinishingOption(int optionId)
    {
        return FinishingOptionIds.Contains(optionId);
    }

    /// <summary>
    ///     The dialogue events worth reporting for a pick, best first: the events that leave the player one finishing
    ///     tap from the end, then every other event in authored order. The per-event Opt file that pairs an option
    ///     with its event is absent from the dump, so every returned event does end the talk.
    /// </summary>
    public List<int> EventsForOption(int optionId)
    {
        if (IsFinishingOption(optionId) || _eventOrder.Count == 0) return [];
        string[] seeds = [_optionTrigger.GetValueOrDefault(optionId, "")];
        List<int> near = [], rest = [];
        foreach (var eventId in _eventOrder)
            if (ClosesAfterReport(seeds, eventId)) near.Add(eventId);
            else rest.Add(eventId);
        return [.. near, .. rest];
    }

    public bool ClosesOnReport(int optionId)
    {
        if (IsFinishingOption(optionId)) return true;
        var events = EventsForOption(optionId);
        return events.Count > 0 && ClosesAfterReport([_optionTrigger.GetValueOrDefault(optionId, "")], events[0]);
    }

    private bool ClosesAfterReport(string[] seeds, int eventId)
    {
        var (fired, options, finished) = Closure(seeds, _eventStrings.GetValueOrDefault(eventId, ""));
        return finished || fired.Contains(TalkEnd) || options.Any(IsFinishingOption);
    }

    // Walks client-only hops from fired strings, never crossing a "triggerEvent_" hop (that needs a report) —
    // except `start`, the string a report itself fires.
    private (HashSet<string> Fired, HashSet<int> Options, bool Finished) Closure(string[] seeds, string start)
    {
        HashSet<string> fired = [], seen = [];
        HashSet<int> options = [];
        Queue<string> queue = new();
        if (start.Length > 0)
        {
            queue.Enqueue(start);
            seen.Add(start);
        }

        void Push(string value)
        {
            if (value.Length == 0 || value == start || value.StartsWith(EventPrefix, StringComparison.Ordinal) ||
                !seen.Add(value)) return;
            queue.Enqueue(value);
        }

        foreach (var seed in seeds) Push(seed);
        var finished = false;
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            fired.Add(current);
            if (!_byWait.TryGetValue(current, out var branches)) continue;
            foreach (var branch in branches)
            {
                if (branch.Finishes) finished = true;
                foreach (var trigger in branch.Triggers) Push(trigger);
                foreach (var option in branch.Options) options.Add(option.OptionId);
            }
        }

        return (fired, options, finished);
    }
}
