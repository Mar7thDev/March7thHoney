using CastoriceHoney.Server.Enums.GridFight;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CastoriceHoney.Server.Data.Excel;

[ResourceEntity("GridFightTraitBonus.json")]
public class GridFightTraitBonusExcel : ExcelResource
{
    public uint ID { get; set; }
    public uint BonusThreshold { get; set; }
    public List<uint> BonusParamList { get; set; } = [];

    [JsonConverter(typeof(StringEnumConverter))]
    public GridFightTraitBonusTypeEnum BonusType { get; set; }

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightTraitBonusData.TryAdd(ID, []);
        GameData.GridFightTraitBonusData[ID].TryAdd(BonusThreshold, this);
    }
}