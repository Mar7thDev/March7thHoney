using SqlSugar;

namespace March7thHoney.Database.Account;

[SugarTable("AuthAttempt")]
public class AuthAttemptData : BaseDatabaseDataHelper
{
    [SugarColumn(IsNullable = false, Length = 128)]
    public string AttemptKey { get; set; } = string.Empty;

    [SugarColumn(IsNullable = false, Length = 32)]
    public string Action { get; set; } = string.Empty;

    public long WindowStart { get; set; }
    public int Count { get; set; }
    public long BlockedUntil { get; set; }
    public long UpdatedAt { get; set; }
}
