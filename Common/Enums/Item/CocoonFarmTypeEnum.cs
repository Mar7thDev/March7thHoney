namespace March7thHoney.Enums.Item;

/// <summary>
///     CocoonConfig.FarmType, numbered the way FarmTypeCocoonFinish FinishWay rows list it in ParamIntList.
///     Grounded values: quest 2100157 "Clear Calyx (Golden)" lists 6/7/8, which are exactly the three golden
///     calyx farm types (the same cocoons quest 2100131 lists by id), and quest 2100158 "Clear Calyx (Crimson)"
///     lists 5 = COCOON3. Values 1-4 follow the client declaration order and are not referenced by any row.
///     Deliberately separate from <see cref="FarmTypeEnum" />, whose values are drop multipliers.
/// </summary>
public enum CocoonFarmTypeEnum
{
    None = 0,
    COCOON = 1,
    COCOON2 = 2,
    ELEMENT = 3,
    RELIC = 4,
    COCOON3 = 5,
    COCOON_AVATAR_EXP = 6,
    COCOON_EQUIPMENT_EXP = 7,
    COCOON_COIN = 8
}
