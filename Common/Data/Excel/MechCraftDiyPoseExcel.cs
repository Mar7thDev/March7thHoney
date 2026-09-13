using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftDiyPose.json")]
[MemoryPackable]
public partial class MechCraftDiyPoseExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("FBKAMIHGLFK")] public string ImagePath { get; set; } = string.Empty;
    [JsonProperty("OHKMNJCINAE")] public int AnimationID { get; set; }

    public override int GetId() => ID;

    public override void Loaded()
    {
        GameData.MechCraftDiyPoseData.TryAdd(ID, this);
    }
}
