using CastoriceHoney.Server.Enums.TournRogue;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CastoriceHoney.Server.Data.Excel;

[ResourceEntity("RogueTournWorkbenchFunc.json")]
public class RogueTournWorkbenchFuncExcel : ExcelResource
{
    public int FuncID { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public RogueTournWorkbenchFuncTypeEnum FuncType { get; set; }

    public override int GetId()
    {
        return FuncID;
    }

    public override void Loaded()
    {
        GameData.RogueTournWorkbenchFuncData.Add(FuncID, this);
    }
}