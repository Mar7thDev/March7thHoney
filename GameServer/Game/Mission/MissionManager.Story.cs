using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.Enums.Quest;

namespace March7thHoney.GameServer.Game.Mission;

public partial class MissionManager
{
    public async ValueTask<StoryStep> AdvanceStory(bool strict)
    {
        var (mainId, phase) = StoryFrontier();
        var step = new StoryStep { MainId = mainId, FromPhase = phase, ToPhase = phase };
        if (mainId == 0)
        {
            step.Note = "no story frontier";
            return step;
        }

        if (phase == StoryFrontierPhase.NotStarted)
        {
            if (!GameData.MainMissionData.TryGetValue(mainId, out var excel))
            {
                step.Note = "frontier is not a configured main mission";
                return step;
            }

            var unmet = UnmetGates(excel);
            if (strict && unmet.Count > 0)
            {
                step.Bypassed = unmet;
                step.Note = "strict: gate unmet";
                return step;
            }

            step.Bypassed = unmet;
            var accepted = await AcceptMainMission(mainId);
            if (accepted.Count == 0 && GetMainMissionStatus(mainId) != MissionPhaseEnum.Accept)
            {
                step.Note = "accept refused";
                return step;
            }

            step.ToPhase = StoryFrontierPhase.Running;
            step.Mutated = true;
        }

        for (var pass = 0; pass < 64; pass++)
        {
            var running = RunningSubsOfMain(mainId);
            if (running.Count == 0) break;
            foreach (var subId in running)
            {
                if (GetSubMissionStatus(subId) != MissionPhaseEnum.Accept) continue;
                await FinishSubMission(subId);
                step.FinishedSubs++;
                step.Mutated = true;
            }
        }

        if (GetMainMissionStatus(mainId) == MissionPhaseEnum.Accept)
        {
            await FinishMainMission(mainId);
            step.Mutated = true;
        }

        if (GetMainMissionStatus(mainId) == MissionPhaseEnum.Finish)
        {
            step.MainDone = true;
            step.ToPhase = StoryFrontierPhase.Finished;
            if (GameData.MainMissionData.TryGetValue(mainId, out var done))
                step.NextMain = done.NextTrackMainMission;
        }
        else
        {
            step.StillRunning = RunningSubsOfMain(mainId);
            if (step.StillRunning.Count > 0) step.Note = "main still open";
        }

        return step;
    }

    public async ValueTask<(int Anchor, int Predecessor, string? Error)> RollbackStory()
    {
        var (frontier, phase) = StoryFrontier();
        if (frontier == 0) return (0, 0, "no story frontier");

        var anchor = phase == StoryFrontierPhase.NotStarted ? StoryPredecessor(frontier) : frontier;
        if (anchor == 0) return (frontier, 0, $"frontier {frontier} has not started and has no predecessor");
        if (!GameData.MainMissionData.TryGetValue(anchor, out var excel))
            return (anchor, 0, $"anchor {anchor} is not a configured main mission");
        if (excel.Type != MainMissionTypeEnum.Main)
            return (anchor, 0, $"anchor {anchor} is {excel.Type}, not Main");

        var predecessor = StoryPredecessor(anchor);
        if (predecessor == 0 || predecessor == anchor)
            return (anchor, predecessor, $"main {anchor} has no predecessor");

        await RemoveMainMission(anchor);
        if (GetMainMissionStatus(predecessor) == MissionPhaseEnum.Finish)
            await RemoveMainMission(predecessor);
        await AcceptMainMission(predecessor);
        Data.TrackingMainMissionId = predecessor;
        MarkDirty();
        return (anchor, predecessor, null);
    }

    public (int MainId, StoryFrontierPhase Phase) StoryFrontier()
    {
        var index = StoryIndex.Current();
        var tracking = Data.TrackingMainMissionId;
        if (tracking > 0 && Data.RunningMainMissionIds.Contains(tracking) && IsMainType(tracking))
            return (tracking, StoryFrontierPhase.Running);

        var bestRunning = index.TieBreak(Data.RunningMainMissionIds);
        if (bestRunning != 0) return (bestRunning, StoryFrontierPhase.Running);

        var deepestFinished = index.TieBreak(Data.FinishedMainMissionIds);
        if (deepestFinished == 0) return (0, StoryFrontierPhase.None);

        if (GameData.MainMissionData.TryGetValue(deepestFinished, out var finished) &&
            finished.NextTrackMainMission > 0 &&
            IsMainType(finished.NextTrackMainMission) &&
            GetMainMissionStatus(finished.NextTrackMainMission) == MissionPhaseEnum.None)
            return (finished.NextTrackMainMission, StoryFrontierPhase.NotStarted);

        return (deepestFinished, StoryFrontierPhase.Finished);
    }

    public int StoryPredecessor(int mainId)
    {
        if (!GameData.MainMissionData.TryGetValue(mainId, out var excel)) return 0;
        var index = StoryIndex.Current();
        var best = 0;
        var bestDepth = -1;
        foreach (var param in excel.TakeParam)
        {
            if (param.Type != MissionTakeTypeEnum.MultiSequence || !IsMainType(param.Value)) continue;
            var depth = index.ChainDepth(param.Value);
            if (depth > bestDepth || (depth == bestDepth && (best == 0 || param.Value < best)))
            {
                best = param.Value;
                bestDepth = depth;
            }
        }

        return best != 0 ? best : index.PreviousTrack(mainId);
    }

    public string DescribeGates(int mainId)
    {
        if (!GameData.MainMissionData.TryGetValue(mainId, out var excel)) return "not a main mission";
        var parts = new List<string>();
        foreach (var param in excel.TakeParam)
            parts.Add(DescribeGate("take", param));
        foreach (var param in excel.BeginParam)
            parts.Add(DescribeGate("begin", param));
        return parts.Count == 0 ? "no gates" : string.Join(", ", parts);
    }

    private List<string> UnmetGates(MainMissionExcel excel)
    {
        var unmet = new List<string>();
        foreach (var param in excel.TakeParam)
            if (!param.IsEqual(Data))
                unmet.Add(DescribeGate("take", param));
        foreach (var param in excel.BeginParam)
            if (!param.IsEqual(Data))
                unmet.Add(DescribeGate("begin", param));
        return unmet;
    }

    private string DescribeGate(string group, MissionParam param)
    {
        var state = param.IsEqual(Data) ? "met" : "unmet";
        return $"{group}={param.Type} {param.Value} {state}";
    }

    private List<int> RunningSubsOfMain(int mainId)
    {
        if (!GameData.MainMissionData.TryGetValue(mainId, out var main)) return [];
        return main.SubMissionIds.Where(id => GetSubMissionStatus(id) == MissionPhaseEnum.Accept).OrderBy(id => id)
            .ToList();
    }

    private static bool IsMainType(int mainId)
    {
        return GameData.MainMissionData.TryGetValue(mainId, out var excel) && excel.Type == MainMissionTypeEnum.Main;
    }

    /// <summary>Anchor for a Prepared main: the closing sub of the chain predecessor.</summary>
    public int PreparedAnchorSubMissionId(int mainId)
    {
        var cursor = mainId;
        for (var hop = 0; hop < 8; hop++)
        {
            var previous = StoryPredecessor(cursor);
            if (previous == 0 || previous == cursor) return 0;
            cursor = previous;
            if (!GameData.MainMissionData.TryGetValue(cursor, out var excel)) continue;

            var closing = 0;
            foreach (var id in excel.MissionInfo.FinishSubMissionList)
                if (id != 0 && GetSubMissionStatus(id) == MissionPhaseEnum.Finish)
                    closing = id;
            if (closing != 0) return closing;

            foreach (var sub in excel.MissionInfo.SubMissionList)
                if (GetSubMissionStatus(sub.ID) == MissionPhaseEnum.Finish)
                    closing = sub.ID;
            if (closing != 0) return closing;
        }

        return 0;
    }
}

public enum StoryFrontierPhase
{
    None,
    NotStarted,
    Running,
    Finished
}

public sealed class StoryStep
{
    public int MainId { get; init; }
    public StoryFrontierPhase FromPhase { get; init; }
    public StoryFrontierPhase ToPhase { get; set; }
    public List<string> Bypassed { get; set; } = [];
    public int FinishedSubs { get; set; }
    public bool MainDone { get; set; }
    public int NextMain { get; set; }
    public string Note { get; set; } = "";
    public bool Mutated { get; set; }
    public List<int> StillRunning { get; set; } = [];

    public override string ToString()
    {
        var text = $"main={MainId} phase={FromPhase}->{ToPhase} finished_subs={FinishedSubs}";
        if (Bypassed.Count > 0) text += $" bypassed=[{string.Join("; ", Bypassed)}]";
        if (MainDone) text += " main=finished";
        if (NextMain > 0) text += $" next={NextMain}";
        if (StillRunning.Count > 0) text += $" still_running=[{string.Join(",", StillRunning)}]";
        if (Note.Length > 0) text += $" ({Note})";
        return text;
    }
}

/// <summary>Memoised backward story graph. Rebuilt when the main-mission table changes size.</summary>
file sealed class StoryIndex
{
    private const int MaxHops = 512;
    private static readonly object Gate = new();
    private static StoryIndex? _current;

    private readonly Dictionary<int, List<int>> _previous = [];
    private readonly Dictionary<int, int> _depth = [];
    private readonly Dictionary<int, int> _trackDepth = [];
    private readonly int _count;

    private StoryIndex(int count)
    {
        _count = count;
        foreach (var (id, excel) in GameData.MainMissionData)
        {
            if (id == 0) continue;
            if (excel.Type == MainMissionTypeEnum.Main && excel.NextTrackMainMission != 0)
                AddPrevious(excel.NextTrackMainMission, id);
            foreach (var param in excel.TakeParam)
            {
                if (param.Type != MissionTakeTypeEnum.MultiSequence || param.Value == 0 || param.Value == id)
                    continue;
                if (GameData.MainMissionData.TryGetValue(param.Value, out var predecessor) &&
                    predecessor.Type == MainMissionTypeEnum.Main)
                    AddPrevious(id, param.Value);
            }
        }

        foreach (var id in GameData.MainMissionData.Keys)
        {
            ChainDepth(id);
            TrackDepth(id);
        }
    }

    public static StoryIndex Current()
    {
        var count = GameData.MainMissionData.Count;
        var cached = _current;
        if (cached != null && cached._count == count) return cached;
        lock (Gate)
        {
            if (_current != null && _current._count == count) return _current;
            _current = new StoryIndex(count);
            return _current;
        }
    }

    public int ChainDepth(int id) => DepthOf(_depth, id, trackOnly: false);

    public int TieBreak(IEnumerable<int> candidates)
    {
        var best = 0;
        var bestDepth = -1;
        var bestTrack = -1;
        foreach (var id in candidates)
        {
            if (!GameData.MainMissionData.TryGetValue(id, out var excel) || excel.Type != MainMissionTypeEnum.Main)
                continue;
            var depth = ChainDepth(id);
            var track = TrackDepth(id);
            var better = depth > bestDepth ||
                         (depth == bestDepth && track > bestTrack) ||
                         (depth == bestDepth && track == bestTrack && (best == 0 || id < best));
            if (!better) continue;
            best = id;
            bestDepth = depth;
            bestTrack = track;
        }

        return best;
    }

    public int PreviousTrack(int mainId)
    {
        var best = 0;
        foreach (var (id, excel) in GameData.MainMissionData)
        {
            if (excel.NextTrackMainMission != mainId || excel.Type != MainMissionTypeEnum.Main) continue;
            if (best == 0 || id < best) best = id;
        }

        return best;
    }

    private int TrackDepth(int id) => DepthOf(_trackDepth, id, trackOnly: true);

    private int DepthOf(Dictionary<int, int> memo, int id, bool trackOnly)
    {
        if (memo.TryGetValue(id, out var known)) return known;
        memo[id] = 0;
        var best = 0;
        if (_previous.TryGetValue(id, out var predecessors))
        {
            foreach (var predecessor in predecessors)
            {
                if (trackOnly &&
                    (!GameData.MainMissionData.TryGetValue(predecessor, out var excel) ||
                     excel.NextTrackMainMission != id))
                    continue;
                var depth = DepthOf(memo, predecessor, trackOnly) + 1;
                if (depth > best) best = depth;
            }
        }

        if (best > MaxHops) best = MaxHops;
        memo[id] = best;
        return best;
    }

    private void AddPrevious(int id, int predecessor)
    {
        if (!_previous.TryGetValue(id, out var list))
        {
            list = [];
            _previous[id] = list;
        }

        if (!list.Contains(predecessor)) list.Add(predecessor);
    }
}
