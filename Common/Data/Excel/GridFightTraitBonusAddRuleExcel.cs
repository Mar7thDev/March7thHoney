using CastoriceHoney.Server.Enums.GridFight;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CastoriceHoney.Server.Data.Excel;

[ResourceEntity("GridFightTraitBonusAddRule.json")]
public class GridFightTraitBonusAddRuleExcel : ExcelResource
{
    public uint ID { get; set; }
    public List<uint> ParamList { get; set; } = [];

    [JsonConverter(typeof(StringEnumConverter))]
    public GridFightTraitBonusAddTypeEnum TraitBonusType { get; set; }

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightTraitBonusAddRuleData.TryAdd(ID, this);
    }
}