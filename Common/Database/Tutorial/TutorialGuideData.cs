using CastoriceHoney.Server.Proto;
using SqlSugar;

namespace CastoriceHoney.Server.Database.Tutorial;

[SugarTable("TutorialGuide")]
public class TutorialGuideData : BaseDatabaseDataHelper
{
    [SugarColumn(IsJson = true)] public Dictionary<int, TutorialStatus> Tutorials { get; set; } = [];
}