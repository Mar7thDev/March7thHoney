using CastoriceHoney.Server.Enums.Task;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CastoriceHoney.Server.Data.Config.Task;

public class TriggerPerformance : TaskConfigInfo
{
    [JsonConverter(typeof(StringEnumConverter))]
    public ELevelPerformanceTypeEnum PerformanceType { get; set; }

    public int PerformanceID { get; set; }
}