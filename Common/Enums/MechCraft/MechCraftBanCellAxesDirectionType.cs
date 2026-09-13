using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Enums.MechCraft;

[JsonConverter(typeof(StringEnumConverter))]
public enum MechCraftBanCellAxesDirectionType
{
    [EnumMember(Value = "NLCDGIPGFDJ")]
    None = 0,
    [EnumMember(Value = "MFJEGBIOEJG")]
    TopLeftToBottomRight = 1,
    [EnumMember(Value = "NFAGMOHNOKG")]
    TopRightToBottomLeft = 2,
    [EnumMember(Value = "GMPPHKBMCKP")]
    BottomLeftToTopRight = 3,
    [EnumMember(Value = "IPPLFIJPHOJ")]
    BottomRightToTopLeft = 4,
}
