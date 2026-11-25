using MemoryPack;
using March7thHoney.Data.Config.Task;
using Newtonsoft.Json.Linq;

namespace March7thHoney.Data.Config;

[MemoryPackable]
public partial class LevelGraphConfigInfo
{
    public List<LevelInitSequeceConfigInfo> OnInitSequece { get; set; } = [];
    public List<LevelStartSequeceConfigInfo> OnStartSequece { get; set; } = [];

    public static LevelGraphConfigInfo LoadFromJsonObject(JObject obj)
    {
        LevelGraphConfigInfo info = new();
        if (obj.ContainsKey(nameof(OnInitSequece)))
            info.OnInitSequece = obj[nameof(OnInitSequece)]
                ?.Select(x => LevelInitSequeceConfigInfo.LoadFromJsonObject((x as JObject)!)).ToList() ?? [];
        if (obj.ContainsKey(nameof(OnStartSequece)))
            info.OnStartSequece = obj[nameof(OnStartSequece)]
                ?.Select(x => LevelStartSequeceConfigInfo.LoadFromJsonObject((x as JObject)!)).ToList() ?? [];
        return info;
    }

    /// <summary>
    ///     Whether this graph completes on a fired custom string: one of its task lists both waits on
    ///     <paramref name="value" /> and ends the performance <paramref name="finishKey" /> ("Mission_&lt;sub id&gt;").
    /// </summary>
    /// <remarks>
    ///     Every wait/finish pair in the 4.7 data sits inside one task list, so a match across lists is not
    ///     accepted. A wait whose list only triggers a performance (sub 202010304 waits "202010351_Start") arms
    ///     a client beat and must never complete the sub.
    /// </remarks>
    public bool FinishesOnCustomString(string value, string finishKey)
    {
        if (value.Length == 0 || finishKey.Length == 0) return false;

        return OnInitSequece.Any(seq => PairsWaitAndFinish(seq.TaskList, value, finishKey)) ||
               OnStartSequece.Any(seq => PairsWaitAndFinish(seq.TaskList, value, finishKey));
    }

    private static bool PairsWaitAndFinish(List<TaskConfigInfo> tasks, string value, string finishKey)
    {
        return tasks.Any(task => task is WaitCustomString wait && wait.CustomString.Value == value) &&
               tasks.Any(task => task is FinishPerformanceMission finish && finish.Key == finishKey);
    }
}
