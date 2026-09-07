using MemoryPack;
using March7thHoney.Proto;

namespace March7thHoney.Database.Tutorial;

[DbTable("TutorialGuide")]
public class TutorialGuideData : BaseDatabaseDataHelper
{
    public Dictionary<int, TutorialStatus> Tutorials { get; set; } = [];

    /// <summary>The TutorialGuideType the client sent per group; the official server echoes it back verbatim.</summary>
    public Dictionary<int, int> Types { get; set; } = [];
}
