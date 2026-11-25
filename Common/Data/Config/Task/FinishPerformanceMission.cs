using MemoryPack;
using Newtonsoft.Json.Linq;

namespace March7thHoney.Data.Config.Task;

/// <summary>The terminal task of a mission graph; <see cref="Key" /> names the sub ("Mission_&lt;id&gt;").</summary>
[MemoryPackable]
public partial class FinishPerformanceMission : TaskConfigInfo
{
    public string Key { get; set; } = "";

    public new static TaskConfigInfo LoadFromJsonObject(JObject obj)
    {
        return new FinishPerformanceMission
        {
            Type = obj[nameof(Type)]?.ToString() ?? "",
            Key = DynamicString.ReadValue(obj[nameof(Key)])
        };
    }
}
