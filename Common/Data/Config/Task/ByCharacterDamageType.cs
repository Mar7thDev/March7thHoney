using CastoriceHoney.Server.Enums.Avatar;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CastoriceHoney.Server.Data.Config.Task;

public class ByCharacterDamageType : PredicateConfigInfo
{
    [JsonConverter(typeof(StringEnumConverter))]
    public DamageTypeEnum DamageType { get; set; } = DamageTypeEnum.Fire;
}