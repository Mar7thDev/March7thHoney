using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Enums.MechCraft;

[JsonConverter(typeof(StringEnumConverter))]
public enum MechCraftRotationType
{
    Ccw0Degrees = 0,
    Ccw90Degrees = 1,
    Ccw180Degrees = 2,
    Ccw270Degrees = 3,
}
