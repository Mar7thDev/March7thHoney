namespace March7thHoney.GameServer.Game.Activity.Activities.DiceCombat;

internal static class DiceCombatConfig
{
    public const uint AvatarIdBegin = 264000;
    public const uint AvatarIdEnd = 264035;
    public const uint DefaultAvatarLevel = 2;

    public static readonly uint[] DefaultDiceIds = { 264101, 264201 };

    public static readonly uint[] CompletedStageIds =
    {
        264402, 264403, 264404, 264405, 264406, 264407, 264408, 264409, 264410, 264411,
        264412, 264413, 264416, 264417, 264418, 264419, 264420, 264421, 264422, 264423,
        264424, 264425, 264426, 264427, 264428
    };

    public static readonly uint[] ScoreTakenIds = { 1001, 1002, 1003, 1004, 1005, 1006 };

    public static readonly uint[] FeatureUnlockIds =
    {
        1001, 1002,
        2009, 2011, 2020, 2024, 2025, 2027, 2028,
        2039, 2043, 2044, 2045, 2046, 2047, 2048,
        2050, 2051, 2052, 2054
    };

    public static readonly (uint StageId, uint Damage)[] StageDamageRecord =
    {
        (2001, 8100),
        (2002, 7600),
        (2003, 8500)
    };
}
