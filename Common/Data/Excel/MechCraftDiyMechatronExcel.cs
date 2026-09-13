using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftDiyMechatron.json")]
[MemoryPackable]
public partial class MechCraftDiyMechatronExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("KHIJNANHBFB")] public int UnlockConditionID { get; set; }
    [JsonProperty("GDBJDAOOCOH")] public int InstanceID { get; set; }
    [JsonProperty("DFKJCMCDMEI")] public bool IsSpecial { get; set; }
    [JsonProperty("GNHGAOHCDNK")] public string PreviewImage { get; set; } = string.Empty;
    [JsonProperty("CDDCLPHPKNC")] public string ArtPrefabPath { get; set; } = string.Empty;
    [JsonProperty("OENAMINOLLF")] public HashName Name { get; set; } = new();
    [JsonProperty("IEFAOJFMIJB")] public List<int> PoseList { get; set; } = [];
    [JsonProperty("MGHLJNKMDJJ")] public List<int> PaintColorList { get; set; } = [];
    [JsonProperty("PNEPAFOAEJD")] public List<int> EffectColorList { get; set; } = [];

    public override int GetId() => ID;

    public override void Loaded()
    {
        GameData.MechCraftDiyMechatronData.TryAdd(ID, this);
    }
}
