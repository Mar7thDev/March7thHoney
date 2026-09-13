using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Enums.MechCraft;

[JsonConverter(typeof(StringEnumConverter))]
public enum MechCraftSymmetryType
{
    None = 0,
    Horizontal = 1,
    Vertical = 2,
}
