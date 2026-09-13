using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Enums.MechCraft;

[JsonConverter(typeof(StringEnumConverter))]
public enum MechCraftDirectionType
{
    None = 0,
    Above = 1,
    Below = 2,
    Left = 3,
    Right = 4,
}
