using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Enums.MechCraft;

[JsonConverter(typeof(StringEnumConverter))]
public enum MechCraftChipTraitType
{
    None = 0,
    Frank = 1,
    Humorous = 2,
    Gentle = 3,
    Steadfast = 4,
    Meticulous = 5,
    Calm = 6,
    Passionate = 7,
    Outgoing = 8,
    Confident = 9,
    Easygoing = 10,
}
