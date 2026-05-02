using CastoriceHoney.Server.Enums.Rogue;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CastoriceHoney.Server.Data.Custom;

public class ChessRogueDiceSurfaceEffectConfig
{
    public int SurfaceId { get; set; }
    public List<ChessRogueDiceSurfaceContentEffect> ContentEffects { get; set; } = [];
}

public class ChessRogueDiceSurfaceContentEffect
{
    [JsonConverter(typeof(StringEnumConverter))]
    public ModifierEffectTypeEnum EffectType { get; set; }

    public Dictionary<string, string> Params { get; set; } = [];
}