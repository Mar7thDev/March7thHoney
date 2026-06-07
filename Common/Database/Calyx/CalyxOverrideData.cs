using MemoryPack;
using March7thHoney.Data.Freesr;

namespace March7thHoney.Database.Calyx;

[DbTable("calyx_override_data")]
public class CalyxOverrideData : BaseDatabaseDataHelper
{
    public bool IsActive { get; set; }
    public string SourceFileName { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public long LoadedAtUnix { get; set; }

    public FreesrCalyxData? CachedJson { get; set; }
}
