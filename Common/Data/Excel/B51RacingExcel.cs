using MemoryPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Data.Excel;

public enum B51RacingPartType { None, Head, Engine, RearWing }

[ResourceEntity("B51RacingCycle.json")]
[MemoryPackable]
public partial class B51RacingCycleExcel : ExcelResource
{
    public int ID { get; set; }
    public List<int> DriverIDList { get; set; } = [];
    public List<int> CarIDList { get; set; } = [];
    public List<int> PaintIDList { get; set; } = [];
    public List<int> PartIDList { get; set; } = [];
    public override int GetId() => ID;
    public override void Loaded() => GameData.B51RacingData.Cycles[ID] = this;
}

[ResourceEntity("B51RacingChallengeCar.json")]
[MemoryPackable]
public partial class B51RacingChallengeCarExcel : ExcelResource
{
    public int ID { get; set; }
    public List<int> PartIDList { get; set; } = [];
    public override int GetId() => ID;
    public override void Loaded() => GameData.B51RacingData.ChallengeCars[ID] = this;
}

[ResourceEntity("B51RacingCar.json")]
[MemoryPackable]
public partial class B51RacingCarExcel : ExcelResource
{
    public int ID { get; set; }
    public string Type { get; set; } = string.Empty;
    public List<int> PartIDList { get; set; } = [];
    public override int GetId() => ID;
    public override void Loaded() => GameData.B51RacingData.Cars[ID] = this;
}

[ResourceEntity("B51RacingPart.json")]
[MemoryPackable]
public partial class B51RacingPartExcel : ExcelResource
{
    public int ID { get; set; }
    [JsonConverter(typeof(StringEnumConverter))] public B51RacingPartType Type { get; set; }
    public override int GetId() => ID;
    public override void Loaded() => GameData.B51RacingData.Parts[ID] = this;
}

[ResourceEntity("B51RacingDriver.json")]
[MemoryPackable]
public partial class B51RacingDriverExcel : ExcelResource
{
    public int ID { get; set; }
    public override int GetId() => ID;
    public override void Loaded() => GameData.B51RacingData.Drivers[ID] = this;
}

[ResourceEntity("B51RacingLivery.json")]
[MemoryPackable]
public partial class B51RacingLiveryExcel : ExcelResource
{
    public int ID { get; set; }
    public override int GetId() => ID;
    public override void Loaded() => GameData.B51RacingData.Paints[ID] = this;
}

[ResourceEntity("B51RacingMatch.json")]
[MemoryPackable]
public partial class B51RacingMatchExcel : ExcelResource
{
    public int ID { get; set; }
    public List<int> TrackIDList { get; set; } = [];
    public override int GetId() => ID;
    public override void Loaded() => GameData.B51RacingData.Matches[ID] = this;
}

[ResourceEntity("B51RacingTrack.json")]
[MemoryPackable]
public partial class B51RacingTrackExcel : ExcelResource
{
    public int ID { get; set; }
    public string LittleGameConfig { get; set; } = string.Empty;
    public int TotalLap { get; set; }
    public override int GetId() => ID;
    public override void Loaded() => GameData.B51RacingData.Tracks[ID] = this;
}
