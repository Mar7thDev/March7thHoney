using MemoryPack;

namespace March7thHoney.Database.Activity;

[DbTable("B51Racing")]
public class B51RacingData : BaseDatabaseDataHelper
{
    public uint CycleId { get; set; }
    public uint LastCycleId { get; set; }
    public uint Day { get; set; }
    public uint CarId { get; set; }
    public uint CarNum { get; set; }
    public uint PaintId { get; set; }
    public uint DriverId { get; set; }
    public uint FinishedMatchNum { get; set; }
    public uint TargetPartId { get; set; }
    public uint DevelopmentActionCount { get; set; }
    public uint ActiveMatchId { get; set; }
    public uint ActiveTrackId { get; set; }
    public uint ActiveRandomSeed { get; set; }
    public List<uint> EquippedPartIds { get; set; } = [];
    public List<uint> DevelopedPartIds { get; set; } = [];
    public List<uint> SelectedDevelopmentActionIds { get; set; } = [];
    public Dictionary<string, int> StatValues { get; set; } = [];
    public Dictionary<uint, uint> TeamPoints { get; set; } = [];
    public List<B51RacingMatchRecordData> MatchRecords { get; set; } = [];
}

[MemoryPackable]
public partial class B51RacingMatchRecordData
{
    public uint MatchId { get; set; }
    public uint Day { get; set; }
    public uint TrackId { get; set; }
    public uint Rank { get; set; }
    public uint FinishTimeMs { get; set; }
}
