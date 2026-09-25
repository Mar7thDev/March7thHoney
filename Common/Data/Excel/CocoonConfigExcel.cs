using MemoryPack;
using March7thHoney.Enums.Item;
using March7thHoney.Util;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("CocoonConfig.json")]
[MemoryPackable]
public partial class CocoonConfigExcel : ExcelResource
{
    public int ID { get; set; }
    public int MappingInfoID { get; set; }
    public int WorldLevel { get; set; }
    public int PropID { get; set; }
    public int StaminaCost { get; set; }
    public int MaxWave { get; set; }
    public List<int> StageIDList { get; set; } = [];
    public List<int> DropList { get; set; } = [];

    // Appended members (MemoryPack: add at the end only). Old caches deserialize them as default.
    [JsonConverter(typeof(SafeStringEnumConverter<CocoonTypeEnum>))]
    public CocoonTypeEnum CocoonType { get; set; }

    [JsonConverter(typeof(SafeStringEnumConverter<CocoonFarmTypeEnum>))]
    public CocoonFarmTypeEnum FarmType { get; set; }

    public override int GetId()
    {
        return ID * 100 + WorldLevel;
    }

    public override void Loaded()
    {
        GameData.CocoonConfigData.Add(GetId(), this);
    }
}
