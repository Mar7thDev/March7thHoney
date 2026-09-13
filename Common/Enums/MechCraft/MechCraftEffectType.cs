using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Enums.MechCraft;

[JsonConverter(typeof(StringEnumConverter))]
public enum MechCraftEffectType
{
    None = 0,
    ExpandBoard = 1,
    CellOverlapCount = 2,
    OrderIncomePercentIncrease = 3,
    UnlockBuffChip = 4,
    BuffChipAdjacentAttributePercentIncrease = 5,
    BuffChipDirectionalAttributePercentIncrease = 6,
    BaseAttributeValue = 7,
    BuffChipFullLineBaseAttributeValueIncrease = 8,
    BuffChipIgnoreOverlapLimit = 9,
    BuffChipOverlappedChipAttributePercentIncrease = 10,
}
