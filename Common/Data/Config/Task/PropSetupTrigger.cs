using Newtonsoft.Json.Linq;

namespace March7thHoney.Data.Config.Task;

public class PropSetupTrigger : TaskConfigInfo
{
    public TargetEvaluator TargetType { get; set; } = new();
    public DynamicFloat TargetGroupID { get; set; } = new();
    public DynamicFloat TargetID { get; set; } = new();
    public string ColliderRelativePath { get; set; } = string.Empty;
    public bool DestroyAfterTriggered { get; set; }
    public List<TaskConfigInfo> OnTriggerEnter { get; set; } = [];

    public new static TaskConfigInfo LoadFromJsonObject(JObject obj)
    {
        PropSetupTrigger info = new();
        info.Type = obj[nameof(Type)]!.ToObject<string>()!;

        if (obj.ContainsKey(nameof(TargetType)))
        {
            var targetType = obj[nameof(TargetType)] as JObject;
            var classType =
                System.Type.GetType(
                    $"March7thHoney.Data.Config.Task.{targetType?["Type"]?.ToString().Replace("RPG.GameCore.", "")}");
            classType ??= typeof(TargetEvaluator);
            info.TargetType = (targetType!.ToObject(classType) as TargetEvaluator)!;
        }

        info.TargetGroupID = obj[nameof(TargetGroupID)]?.ToObject<DynamicFloat>() ?? new DynamicFloat();
        info.TargetID = obj[nameof(TargetID)]?.ToObject<DynamicFloat>() ?? new DynamicFloat();
        info.ColliderRelativePath = obj[nameof(ColliderRelativePath)]?.ToObject<string>() ?? string.Empty;
        info.DestroyAfterTriggered = obj[nameof(DestroyAfterTriggered)]?.ToObject<bool>() ?? false;
        info.OnTriggerEnter = obj[nameof(OnTriggerEnter)]
            ?.Select(x => TaskConfigInfo.LoadFromJsonObject((x as JObject)!)).ToList() ?? [];

        return info;
    }
}
