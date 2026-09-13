using MemoryPack;
using March7thHoney.Enums.Rogue;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("RogueDLCArea.json")]
[MemoryPackable]
public partial class RogueDLCAreaExcel : ExcelResource
{
    public int AreaID { get; set; }
    public string SubType { get; set; } = "";
    public List<int> LayerIDList { get; set; } = [];
    public List<int> DifficultyID { get; set; } = [];
    public int FirstReward { get; set; }

    public List<RogueDLCAreaScoreMap> AreaScoreMap { get; set; } = [];

    [JsonIgnore] public RogueSubModeEnum RogueVersionId { get; set; }

    public override int GetId()
    {
        return AreaID;
    }

    public override void Loaded()
    {
        GameData.RogueDLCAreaData[AreaID] = this;

        RogueVersionId = SubType.Contains("Nous") ? RogueSubModeEnum.ChessRogueNous : RogueSubModeEnum.ChessRogue;
    }
}

[MemoryPackable]
public partial class RogueDLCAreaScoreMap
{
    // 4.5 keys, verified against rows: layer 1/2, explore 1300/5200, finish 2600/7800.
    [JsonProperty("FFAIBNDHOBI")] public int Layer { get; set; }

    [JsonProperty("DICDMGODBCF")] public int ExploreScore { get; set; }

    [JsonProperty("EFGMPBPHGPA")] public int FinishScore { get; set; }
}
