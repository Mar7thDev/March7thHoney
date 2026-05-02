using CastoriceHoney.Server.Enums.GridFight;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CastoriceHoney.Server.Data.Excel;

[ResourceEntity("GridFightTraitEffect.json")]
public class GridFightTraitEffectExcel : ExcelResource
{
    public uint ID { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public GridFightTraitEffectTypeEnum TraitEffectType { get; set; }

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightTraitEffectData.TryAdd(ID, this);
    }
}