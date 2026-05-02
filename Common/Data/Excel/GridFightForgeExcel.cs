using CastoriceHoney.Server.Enums.GridFight;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CastoriceHoney.Server.Data.Excel;

[ResourceEntity("GridFightForge.json")]
public class GridFightForgeExcel : ExcelResource
{
    public uint ID { get; set; }
    public uint EquipNum { get; set; }
    public List<uint> ParamList { get; set; } = [];

    [JsonConverter(typeof(StringEnumConverter))]
    public GridFightForgeFuncTypeEnum FuncType { get; set; }

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightForgeData.TryAdd(ID, this);
    }
}