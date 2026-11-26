using MemoryPack;
using Newtonsoft.Json.Linq;
namespace March7thHoney.Data.Config.Task;

[MemoryPackable]
public partial class TriggerCustomString : TaskConfigInfo
{
    public DynamicString CustomString { get; set; } = new();
}

[MemoryPackable]
public partial class DynamicString
{
    public string Value { get; set; } = string.Empty;

    /// <summary>A string field written either as a plain string or as a dynamic value object {"Value": ...}.</summary>
    public static string ReadValue(JToken? token)
    {
        return token switch
        {
            JValue value => value.ToString(),
            JObject obj => obj[nameof(Value)]?.ToString() ?? string.Empty,
            _ => string.Empty
        };
    }
}
