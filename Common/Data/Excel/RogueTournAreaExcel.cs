using MemoryPack;
using March7thHoney.Enums.TournRogue;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Data.Excel;

[ResourceEntity("RogueTournArea.json")]
[MemoryPackable]
public partial class RogueTournAreaExcel : ExcelResource
{
    public List<int> MonsterDisplayItemList { get; set; } = [];
    public List<int> LayerIDList { get; set; } = [];
    public List<int> DifficultyIDList { get; set; } = [];
    public int WorldLevelLimit { get; set; }
    public int FirstReward { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public RogueTournDifficultyTypeEnum Difficulty { get; set; }

    public int ExpScoreID { get; set; }
    public int UnlockID { get; set; }
    public int AreaID { get; set; }
    public int DivisionLevel { get; set; }
    public HashName AreaNameID { get; set; } = new();
    public bool IsHard { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public RogueTournModeEnum TournMode { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public RogueTournAreaGroupIDEnum AreaGroupID { get; set; }

    // 4.5 aliases. Keep the 4.6 property names above as the primary JSON contract.
    [JsonProperty("GLNDIILFKBN")] private List<int>? LayerIdList45 { set => LayerIDList = value ?? []; }
    [JsonProperty("EODCEHDOAEB")] private List<int>? DifficultyIdList45 { set => DifficultyIDList = value ?? []; }
    [JsonProperty("IMNLCDOMMOG")] private int FirstReward45 { set => FirstReward = value; }
    [JsonProperty("GOEDJMNFALN"), JsonConverter(typeof(StringEnumConverter))]
    private RogueTournDifficultyTypeEnum Difficulty45 { set => Difficulty = value; }
    [JsonProperty("JJKLIJNFIBB")] private int ExpScoreId45 { set => ExpScoreID = value; }
    [JsonProperty("FOMEIPIEGII")] private int UnlockId45 { set => UnlockID = value; }
    [JsonProperty("BEOFPCAACEP")] private int AreaId45 { set => AreaID = value; }
    [JsonProperty("NKLFPMKHELN")] private int DivisionLevel45 { set => DivisionLevel = value; }
    [JsonProperty("PCBLHKODOMG")] private bool IsHard45 { set => IsHard = value; }
    [JsonProperty("HILINOJPLGA"), JsonConverter(typeof(StringEnumConverter))]
    private RogueTournModeEnum TournMode45 { set => TournMode = value; }
    [JsonProperty("PJGJLMIODBD"), JsonConverter(typeof(StringEnumConverter))]
    private RogueTournAreaGroupIDEnum AreaGroupId45 { set => AreaGroupID = value; }

    public override int GetId()
    {
        return AreaID;
    }

    public override void Loaded()
    {
        GameData.RogueTournAreaData.TryAdd(AreaID, this);
    }
}
