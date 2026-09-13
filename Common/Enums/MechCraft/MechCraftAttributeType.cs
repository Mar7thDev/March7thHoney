using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Enums.MechCraft;

[JsonConverter(typeof(StringEnumConverter))]
public enum MechCraftAttributeType
{
    [EnumMember(Value = "NLCDGIPGFDJ")]
    None = 0,
    [EnumMember(Value = "HMFKENCHCOK")]
    Intelligence = 1,
    [EnumMember(Value = "JLOOPPGEADG")]
    Energy = 2,
    [EnumMember(Value = "OMMLLNKIHNJ")]
    Charm = 3,
    [EnumMember(Value = "AIDNPNEBLII")]
    Strength = 4,
    [EnumMember(Value = "GCJMFBKLGHI")]
    Speed = 5,
}
