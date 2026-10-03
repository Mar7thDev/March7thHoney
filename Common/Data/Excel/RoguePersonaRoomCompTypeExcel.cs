using MemoryPack;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

/// <summary>
///     The client's room-kind table: which RogueTournRoomType a composition id renders as (Reforge is 21, Blank 22,
///     Conversion 23, Puman 24, Hidden owns 12/13/14/15/17). Columns are obfuscated in the 4.6 dump.
/// </summary>
[ResourceEntity("RoguePersonaRoomCompType.json")]
[MemoryPackable]
public partial class RoguePersonaRoomCompTypeExcel : ExcelResource
{
    [JsonProperty("LLICIMBCNPF")] public int CompositionType { get; set; }
    [JsonProperty("HCBADDHNIDG")] public int VariantKind { get; set; }

    // RogueTournRoomTypeEnum name ("Boss", "Reforge", ...).
    [JsonProperty("LHLKJIDFLIN")] public string RoomType { get; set; } = "";
    [JsonProperty("JPLIONFJGCL")] public string DoorColor { get; set; } = "";

    public override int GetId()
    {
        return CompositionType;
    }

    public override void Loaded()
    {
        GameData.RoguePersonaRoomCompTypeData.TryAdd(CompositionType, this);
    }
}
