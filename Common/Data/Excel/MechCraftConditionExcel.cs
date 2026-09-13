using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftCondition.json")]
[MemoryPackable]
public partial class MechCraftConditionExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("GMPGDEINODK")] public MechCraftConditionType Type { get; set; }
    [JsonProperty("PBLPLDJKPEI")] public List<int> ParamList { get; set; } = [];

    public override int GetId() => ID;

    public override void Loaded()
    {
        GameData.MechCraftConditionData.TryAdd(ID, this);
    }
}
