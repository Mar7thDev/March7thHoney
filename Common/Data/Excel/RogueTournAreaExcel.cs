using MemoryPack;
using March7thHoney.Enums.TournRogue;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Data.Excel;

// 4.5 ships RogueTournArea with obfuscated keys; unmapped fields have no verified key.
[ResourceEntity("RogueTournArea.json")]
[MemoryPackable]
public partial class RogueTournAreaExcel : ExcelResource
{
    public List<int> MonsterDisplayItemList { get; set; } = [];
    [JsonProperty("GLNDIILFKBN")] public List<int> LayerIDList { get; set; } = [];
    [JsonProperty("EODCEHDOAEB")] public List<int> DifficultyIDList { get; set; } = [];
    public int WorldLevelLimit { get; set; }
    [JsonProperty("IMNLCDOMMOG")] public int FirstReward { get; set; }

    [JsonProperty("GOEDJMNFALN")]
    [JsonConverter(typeof(StringEnumConverter))]
    public RogueTournDifficultyTypeEnum Difficulty { get; set; }

    [JsonProperty("JJKLIJNFIBB")] public int ExpScoreID { get; set; }
    [JsonProperty("FOMEIPIEGII")] public int UnlockID { get; set; }
    [JsonProperty("BEOFPCAACEP")] public int AreaID { get; set; }
    [JsonProperty("NKLFPMKHELN")] public int DivisionLevel { get; set; }
    public HashName AreaNameID { get; set; } = new();
    [JsonProperty("PCBLHKODOMG")] public bool IsHard { get; set; }

    [JsonProperty("HILINOJPLGA")]
    [JsonConverter(typeof(StringEnumConverter))]
    public RogueTournModeEnum TournMode { get; set; }

    [JsonProperty("PJGJLMIODBD")]
    [JsonConverter(typeof(StringEnumConverter))]
    public RogueTournAreaGroupIDEnum AreaGroupID { get; set; }

    public override int GetId()
    {
        return AreaID;
    }

    public override void Loaded()
    {
        GameData.RogueTournAreaData.TryAdd(AreaID, this);
    }
}
