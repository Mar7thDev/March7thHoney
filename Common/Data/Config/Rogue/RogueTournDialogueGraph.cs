using MemoryPack;
using Newtonsoft.Json.Linq;

namespace March7thHoney.Data.Config.Rogue;

/// <summary>
///     The parts of a Divergent Universe event's Act graph (RogueDialogue/RogueEventTourn*/Event*/Act*.json) the server
///     needs to answer the RogueCommon dialogue: every branch with the custom string that wakes it, the strings it
///     fires, the options it shows, the dialogue events it accepts and whether it ends the graph. The client runs the
///     graph itself; the per-event Opt*.json is absent from the 4.6 dump, so this is the only option source.
/// </summary>
[MemoryPackable]
public partial class RogueTournDialogueGraph
{
    public int NpcId { get; set; }
    public int TalkNameId { get; set; }
    public List<RogueTournDialogueBranch> Branches { get; set; } = [];

    private const string WaitNode = "WaitCustomString";
    private const string TriggerNode = "TriggerCustomString";
    private const string OptionNode = "PlayRogueOptionTalk";
    private const string EventNode = "WaitDialogueEvent";
    private const string FinishNode = "FinishLevelGraph";

    public static RogueTournDialogueGraph? Parse(int npcId, int talkNameId, string json)
    {
        var root = JObject.Parse(json);
        if (root["OnStartSequece"] is not JArray sequences) return null;

        var graph = new RogueTournDialogueGraph { NpcId = npcId, TalkNameId = talkNameId };
        foreach (var sequence in sequences.OfType<JObject>())
        {
            var branch = new RogueTournDialogueBranch();
            if (sequence["TaskList"] is JArray tasks) ReadTasks(tasks, branch);
            graph.Branches.Add(branch);
        }

        return graph;
    }

    private static void ReadTasks(JArray tasks, RogueTournDialogueBranch branch)
    {
        foreach (var task in tasks.OfType<JObject>())
        {
            var type = (task["$type"] ?? task["Type"])?.Value<string>() ?? "";
            type = type.Replace("RPG.GameCore.", "");
            switch (type)
            {
                case WaitNode:
                    branch.Wait = CustomString(task["CustomString"]);
                    break;
                case TriggerNode:
                    var fired = CustomString(task["CustomString"]);
                    if (fired.Length > 0) branch.Triggers.Add(fired);
                    break;
                case OptionNode:
                    foreach (var option in (task["OptionList"] as JArray ?? []).OfType<JObject>())
                    {
                        var optionId = option["RogueOptionID"]?.Value<int>() ?? 0;
                        if (optionId == 0) continue;
                        branch.Options.Add(new RogueTournDialogueOption
                        {
                            OptionId = optionId,
                            Trigger = option["TriggerCustomString"]?.Value<string>() ?? ""
                        });
                    }

                    break;
                case EventNode:
                    foreach (var dialogueEvent in (task["DialogueEventList"] as JArray ?? []).OfType<JObject>())
                    {
                        var eventId = dialogueEvent["DialogueEventID"]?.Value<int>() ?? 0;
                        if (eventId == 0) continue;
                        branch.Events.Add(new RogueTournDialogueEvent
                        {
                            EventId = eventId,
                            Success = dialogueEvent["SuccessCustomString"]?.Value<string>() ?? ""
                        });
                    }

                    break;
                case FinishNode:
                    branch.Finishes = true;
                    break;
            }
        }
    }

    private static string CustomString(JToken? token)
    {
        return token switch
        {
            JObject obj => obj["Value"]?.Value<string>() ?? "",
            JValue value => value.Value<string>() ?? "",
            _ => ""
        };
    }
}

[MemoryPackable]
public partial class RogueTournDialogueBranch
{
    // Empty = the opening branch that runs when the graph starts.
    public string Wait { get; set; } = "";
    public List<string> Triggers { get; set; } = [];
    public List<RogueTournDialogueOption> Options { get; set; } = [];
    public List<RogueTournDialogueEvent> Events { get; set; } = [];
    public bool Finishes { get; set; }
}

[MemoryPackable]
public partial class RogueTournDialogueOption
{
    public int OptionId { get; set; }
    public string Trigger { get; set; } = "";
}

[MemoryPackable]
public partial class RogueTournDialogueEvent
{
    public int EventId { get; set; }
    public string Success { get; set; } = "";
}
