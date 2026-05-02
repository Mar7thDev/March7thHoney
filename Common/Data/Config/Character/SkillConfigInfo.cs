using CastoriceHoney.Server.Enums.Avatar;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CastoriceHoney.Server.Data.Config.Character;

public class SkillConfigInfo
{
    public string EntryAbility { get; set; } = "";

    [JsonConverter(typeof(StringEnumConverter))]
    public SkillUseTypeEnum UseType { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public AdventureSkillTypeEnum AdventureSkillType { get; set; }
}