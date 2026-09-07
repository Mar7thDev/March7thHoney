using MemoryPack;
using Newtonsoft.Json.Linq;

namespace March7thHoney.Data.Config.Task;

/// <summary>
///     The collider trigger a level graph arms on a prop. Only the target is kept: it tells the server which
///     prop a Talk/PropState sub-mission is about when the sub carries no ParamInt1/ParamInt2 of its own
///     (see LevelTask.ResolveGraphPropTarget).
/// </summary>
[MemoryPackable]
public partial class PropSetupTrigger : TaskConfigInfo
{
    public TargetEvaluator TargetType { get; set; } = new UnknownTargetEvaluator();

    public new static TaskConfigInfo LoadFromJsonObject(JObject obj)
    {
        PropSetupTrigger info = new()
        {
            Type = obj[nameof(Type)]!.ToObject<string>()!
        };

        if (obj.ContainsKey(nameof(TargetType)))
        {
            var targetType = obj[nameof(TargetType)] as JObject;
            var classType =
                System.Type.GetType(
                    $"March7thHoney.Data.Config.Task.{targetType?["Type"]?.ToString().Replace("RPG.GameCore.", "")}");
            classType ??= System.Type.GetType("March7thHoney.Data.Config.Task.UnknownTargetEvaluator");
            info.TargetType = (targetType!.ToObject(classType!) as TargetEvaluator)!;
        }

        return info;
    }
}
