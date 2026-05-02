using CastoriceHoney.Server.Proto;
using SqlSugar;

namespace CastoriceHoney.Server.Database.Tutorial;

[SugarTable("Tutorial")]
public class TutorialData : BaseDatabaseDataHelper
{
    [SugarColumn(IsJson = true)] public Dictionary<int, TutorialStatus> Tutorials { get; set; } = [];
}