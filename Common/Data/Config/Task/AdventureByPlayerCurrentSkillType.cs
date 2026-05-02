using CastoriceHoney.Server.Enums.Avatar;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CastoriceHoney.Server.Data.Config.Task;

public class AdventureByPlayerCurrentSkillType : PredicateConfigInfo
{
    [JsonConverter(typeof(StringEnumConverter))]
    public AdventureSkillTypeEnum SkillType { get; set; }
}