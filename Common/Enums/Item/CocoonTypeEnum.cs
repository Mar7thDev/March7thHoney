namespace March7thHoney.Enums.Item;

/// <summary>
///     CocoonConfig.CocoonType. The numeric values are what AnyCocoonFinish FinishWay rows list in
///     ParamIntList: quest 2100102 "Clear Cavern of Corrosion" and mission 401012601 (Cavern of Corrosion) use
///     3, quest 2100104 "Complete Echo of War" uses 2, and the calyx achievement ladders use 1.
/// </summary>
public enum CocoonTypeEnum
{
    TYPE_NONE = 0,
    TYPE_NORMAL = 1,
    TYPE_WEEK = 2,
    TYPE_RELIC = 3
}
