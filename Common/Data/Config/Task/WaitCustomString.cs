using MemoryPack;
using Newtonsoft.Json.Linq;

namespace March7thHoney.Data.Config.Task;

/// <summary>
///     A level-graph wait on a custom string. The server never blocks on it; together with a terminal
///     FinishPerformanceMission in the same task list it tells which running sub a fired string completes
///     (see <see cref="LevelGraphConfigInfo.FinishesOnCustomString" />).
/// </summary>
[MemoryPackable]
public partial class WaitCustomString : TaskConfigInfo
{
    public DynamicString CustomString { get; set; } = new();

    public new static TaskConfigInfo LoadFromJsonObject(JObject obj)
    {
        return new WaitCustomString
        {
            Type = obj[nameof(Type)]?.ToString() ?? "",
            CustomString = new DynamicString { Value = DynamicString.ReadValue(obj[nameof(CustomString)]) }
        };
    }
}
