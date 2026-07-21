using MemoryPack;

namespace March7thHoney.Data.Excel;

[ResourceEntity("B51RacingAgenda.json")]
[MemoryPackable]
public partial class B51RacingAgendaExcel : ExcelResource
{
    public int CycleID { get; set; }
    public int Day { get; set; }
    public string ContentType { get; set; } = "";

    public override int GetId() => CycleID * 100 + Day;

    public override void Loaded() => GameData.B51RacingAgendaData.TryAdd(GetId(), this);
}

[ResourceEntity("B51RacingCar.json")]
[MemoryPackable]
public partial class B51RacingCarExcel : ExcelResource
{
    public int ID { get; set; }
    public string Type { get; set; } = "";
    public int TeamID { get; set; }
    public Dictionary<string, int> StatValueMap { get; set; } = [];
    public List<int> SkillIDList { get; set; } = [];
    public List<int> LiveryIDList { get; set; } = [];
    public List<int> PartIDList { get; set; } = [];
    public int CarNumber { get; set; }
    public int AIRole { get; set; }
    public string InitHexColor { get; set; } = "";

    public override int GetId() => ID;

    public override void Loaded() => GameData.B51RacingCarData.TryAdd(ID, this);
}

[ResourceEntity("B51RacingChallengeCar.json")]
[MemoryPackable]
public partial class B51RacingChallengeCarExcel : ExcelResource
{
    public int ID { get; set; }
    public List<int> PartIDList { get; set; } = [];

    public override int GetId() => ID;

    public override void Loaded() => GameData.B51RacingChallengeCarData.TryAdd(ID, this);
}

[ResourceEntity("B51RacingCycle.json")]
[MemoryPackable]
public partial class B51RacingCycleExcel : ExcelResource
{
    public int ID { get; set; }
    public HashName Name { get; set; } = new();
    public List<int> DriverIDList { get; set; } = [];
    public List<int> CarIDList { get; set; } = [];
    public List<int> PaintIDList { get; set; } = [];
    public List<int> DecalIDList { get; set; } = [];
    public List<int> PartIDList { get; set; } = [];
    public List<int> TeamIDList { get; set; } = [];

    public override int GetId() => ID;

    public override void Loaded() => GameData.B51RacingCycleData.TryAdd(ID, this);
}

[ResourceEntity("B51RacingDevelopmentAction.json")]
[MemoryPackable]
public partial class B51RacingDevelopmentActionExcel : ExcelResource
{
    public int ID { get; set; }
    public HashName Name { get; set; } = new();
    public Dictionary<string, int> AddStatValueMap { get; set; } = [];

    public override int GetId() => ID;

    public override void Loaded() => GameData.B51RacingDevelopmentActionData.TryAdd(ID, this);
}

[ResourceEntity("B51RacingDriver.json")]
[MemoryPackable]
public partial class B51RacingDriverExcel : ExcelResource
{
    public int ID { get; set; }
    public HashName Name { get; set; } = new();
    public HashName Desc { get; set; } = new();
    public string AvatarIconPath { get; set; } = "";
    public Dictionary<string, int> AddStatMap { get; set; } = [];

    public override int GetId() => ID;

    public override void Loaded() => GameData.B51RacingDriverData.TryAdd(ID, this);
}

[ResourceEntity("B51RacingLivery.json")]
[MemoryPackable]
public partial class B51RacingLiveryExcel : ExcelResource
{
    public int ID { get; set; }
    public HashName Name { get; set; } = new();
    public string Type { get; set; } = "";
    public string AssetPath { get; set; } = "";
    public string IconPath { get; set; } = "";

    public override int GetId() => ID;

    public override void Loaded() => GameData.B51RacingLiveryData.TryAdd(ID, this);
}

[ResourceEntity("B51RacingMatch.json")]
[MemoryPackable]
public partial class B51RacingMatchExcel : ExcelResource
{
    public int ID { get; set; }
    public HashName Name { get; set; } = new();
    public List<int> EnemyCarIDList { get; set; } = [];
    public List<int> TrackIDList { get; set; } = [];
    public int LockDriverID { get; set; }
    public List<int> TeamRankList { get; set; } = [];
    public bool IsTutorial { get; set; }

    public override int GetId() => ID;

    public override void Loaded() => GameData.B51RacingMatchData.TryAdd(ID, this);
}

[ResourceEntity("B51RacingPart.json")]
[MemoryPackable]
public partial class B51RacingPartExcel : ExcelResource
{
    public int ID { get; set; }
    public HashName Name { get; set; } = new();
    public string Type { get; set; } = "";
    public Dictionary<string, int> AddStatTierMap { get; set; } = [];
    public int SkillID { get; set; }
    public string AssetPath { get; set; } = "";
    public string IconPath { get; set; } = "";
    public bool IsDefault { get; set; }

    public override int GetId() => ID;

    public override void Loaded() => GameData.B51RacingPartData.TryAdd(ID, this);
}

[ResourceEntity("B51RacingSkill.json")]
[MemoryPackable]
public partial class B51RacingSkillExcel : ExcelResource
{
    public int ID { get; set; }
    public int Level { get; set; }
    public string Type { get; set; } = "";
    public HashName Name { get; set; } = new();
    public HashName Desc { get; set; } = new();
    public HashName Desc_Front { get; set; } = new();
    public HashName Desc_Back { get; set; } = new();
    public string IconPath { get; set; } = "";
    public string IconPath_128 { get; set; } = "";
    public string VideoPath { get; set; } = "";
    public List<double> ParamList { get; set; } = [];
    public List<string> PassiveList { get; set; } = [];
    public List<double> PassiveParamList { get; set; } = [];

    public override int GetId() => ID * 10 + Level;

    public override void Loaded() => GameData.B51RacingSkillData.TryAdd(GetId(), this);
}

[ResourceEntity("B51RacingStat.json")]
[MemoryPackable]
public partial class B51RacingStatExcel : ExcelResource
{
    public string ID { get; set; } = "";
    public List<int> TierRequireValueList { get; set; } = [];
    public double ExtraRatio { get; set; }
    public HashName Name { get; set; } = new();
    public HashName Desc { get; set; } = new();
    public string IconPath { get; set; } = "";

    public override int GetId() => StatTypeToId(ID);

    public override void Loaded() => GameData.B51RacingStatData.TryAdd(GetId(), this);

    public static int StatTypeToId(string statType) => statType switch
    {
        "Speed" => 1,
        "Acceleration" => 2,
        "Aggressiveness" => 3,
        "Drift" => 4,
        "Charge" => 5,
        _ => 0
    };
}

[ResourceEntity("B51RacingTeam.json")]
[MemoryPackable]
public partial class B51RacingTeamExcel : ExcelResource
{
    public int ID { get; set; }
    public HashName Name { get; set; } = new();
    public string IconPath { get; set; } = "";
    public int InitialPoint { get; set; }

    public override int GetId() => ID;

    public override void Loaded() => GameData.B51RacingTeamData.TryAdd(ID, this);
}

[ResourceEntity("B51RacingTrack.json")]
[MemoryPackable]
public partial class B51RacingTrackExcel : ExcelResource
{
    public int ID { get; set; }
    public HashName Name { get; set; } = new();
    public string LittleGameConfig { get; set; } = "";
    public string MinimapPath { get; set; } = "";
    public string MinimapPath_Dark { get; set; } = "";
    public int TotalLap { get; set; }
    public HashName RecordDriverName { get; set; } = new();
    public string RecordDriverIconPath { get; set; } = "";
    public double RecordTime { get; set; }

    public override int GetId() => ID;

    public override void Loaded() => GameData.B51RacingTrackData.TryAdd(ID, this);
}
