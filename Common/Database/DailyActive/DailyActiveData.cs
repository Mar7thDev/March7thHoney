namespace March7thHoney.Database.DailyActive;

/// <summary>
///     One player's daily-training window (每日实训). Everything except <see cref="Nonce" /> and
///     <see cref="MissionsTotal" /> belongs to <see cref="DayKey" /> and is replaced when the day rolls over.
///     New table: old databases simply create it empty on first start.
/// </summary>
[DbTable("DailyActive")]
public class DailyActiveData : BaseDatabaseDataHelper
{
    /// <summary>Local server date (yyyyMMdd) of the window, shifted back a day before the reset hour. 0 = never rolled.</summary>
    public int DayKey { get; set; }

    /// <summary>Seed salt bumped only by the GM reset, so a reconnect can never re-roll the day's set.</summary>
    public int Nonce { get; set; }

    /// <summary>Today's rolled QuestType 5 quest ids.</summary>
    public List<int> QuestIds { get; set; } = [];

    /// <summary>Today's daily active points (virtual item 23).</summary>
    public int Point { get; set; }

    /// <summary>Tier levels already claimed today.</summary>
    public List<int> TakenLevels { get; set; } = [];

    /// <summary>Daily main missions finished in this window (FinishTodayDailyMissionCnt).</summary>
    public List<int> MissionsToday { get; set; } = [];

    /// <summary>Daily main missions already offered in this window, so repeated rolls top up instead of adding more.</summary>
    public List<int> MissionsOffered { get; set; } = [];

    /// <summary>Daily main missions ever finished (FinishDailyMissionCnt). Survives the rollover.</summary>
    public int MissionsTotal { get; set; }
}
