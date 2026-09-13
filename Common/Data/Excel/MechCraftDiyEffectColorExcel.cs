using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftDiyEffectColor.json")]
[MemoryPackable]
public partial class MechCraftDiyEffectColorExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("FBKAMIHGLFK")] public string ImagePath { get; set; } = string.Empty;
    [JsonProperty("CADLBIKGCCP")] public MechCraftAttributeType AttributeType { get; set; }
    [JsonProperty("FIPKFIOMAJI")] public string HeadEffectPath { get; set; } = string.Empty;
    [JsonProperty("PNFOMBOEKJN")] public string ElbowLEffectPath { get; set; } = string.Empty;
    [JsonProperty("KGHMLPKFDNG")] public string ShoulderLEffectPath { get; set; } = string.Empty;
    [JsonProperty("EILBLKKAIDJ")] public string ElbowREffectPath { get; set; } = string.Empty;
    [JsonProperty("ACPJGPFJAOO")] public string ShoulderREffectPath { get; set; } = string.Empty;
    [JsonProperty("ADIIIAFGFJB")] public string KneePartLEffectPath { get; set; } = string.Empty;
    [JsonProperty("JCNLGFPDCBL")] public string HipLEffectPath { get; set; } = string.Empty;
    [JsonProperty("DDLJDMFBDJA")] public string KneePartREffectPath { get; set; } = string.Empty;
    [JsonProperty("JFNMGODGCAA")] public string HipREffectPath { get; set; } = string.Empty;
    [JsonProperty("IEJKJKDFGAG")] public string ChestEffectPath { get; set; } = string.Empty;
    [JsonProperty("KCLDOCJPEPO")] public string ItemEffectPath { get; set; } = string.Empty;

    public override int GetId() => ID;

    public override void Loaded()
    {
        GameData.MechCraftDiyEffectColorData.TryAdd(ID, this);
    }
}
