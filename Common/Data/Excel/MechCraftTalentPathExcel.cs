using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftTalentPath.json")]
[MemoryPackable]
public partial class MechCraftTalentPathExcel : ExcelResource
{
    [JsonProperty("PHFMCACHFIJ")] public int ID { get; set; }
    [JsonProperty("EIDBOMGAMGO")] public List<int> TalentPointIDList { get; set; } = [];
    [JsonProperty("CLCBJFKDMKH")] public int UnlockByFinishSubMissionID { get; set; }
    [JsonProperty("NMAHGFAPENI")] public HashName Desc { get; set; } = new();
    [JsonProperty("GNNFCCGDJGD")] public HashName PartnerTalk { get; set; } = new();
    [JsonProperty("ELBPOILJNLF")] public int PartnerActorID { get; set; }
    [JsonProperty("NOKHLKDMJDD")] public HashName AssistantTalk { get; set; } = new();
    [JsonProperty("EFEAGFOILKE")] public HashName AssistantPreTalk { get; set; } = new();
    [JsonProperty("MDHIHFOKECL")] public HashName UnlockTalk { get; set; } = new();
    [JsonProperty("LHPGDMNMDPI")] public HashName LockedTalk { get; set; } = new();

    public override int GetId() => ID;

    public override void Loaded()
    {
        GameData.MechCraftTalentPathData.TryAdd(ID, this);
    }
}
