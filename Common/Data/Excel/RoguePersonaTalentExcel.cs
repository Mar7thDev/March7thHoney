using MemoryPack;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("RoguePersonaTalent.json")]
[MemoryPackable]
public partial class RoguePersonaTalentExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("DBALOLNOLGL")] public int GroupID { get; set; }
    [JsonProperty("AAGKEBFHLMC")] public int Level { get; set; }
    [JsonProperty("DPCMGDIIAKN")] public int PreTalentID { get; set; }

    public override int GetId()
    {
        return ID;
    }

    public override void Loaded()
    {
        GameData.RoguePersonaTalentData.TryAdd(ID, this);
    }
}
