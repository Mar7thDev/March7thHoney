using CastoriceHoney.Server.Enums.Scene;
using CastoriceHoney.Server.Util;
using Newtonsoft.Json;

namespace CastoriceHoney.Server.Data.Excel;

[ResourceEntity("MazeChest.json")]
public class MazeChestExcel : ExcelResource
{
    public int WorldID { get; set; }
    public int ID { get; set; }

    [JsonConverter(typeof(SafeEnumListConverter<ChestTypeEnum>))]
    public List<ChestTypeEnum> ChestType { get; set; } = [];

    public override int GetId()
    {
        return ID;
    }

    public override void Loaded()
    {
        GameData.MazeChestData.Add(ID, this);
    }
}
