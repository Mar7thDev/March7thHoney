using MemoryPack;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

/// <summary>
///     The Persona (Divergent Universe Tourn3) layer plan: one row per room slot of a layer and the room preset that
///     slot always plays (0 = the slot's card is drawn at runtime). The columns are still obfuscated in the 4.6 dump;
///     the row count per layer equals the official run's room count for that layer (3001 → 4, 3002 → 5, 3012 → 7).
/// </summary>
[ResourceEntity("RoguePersonaLayerRoom.json")]
[MemoryPackable]
public partial class RoguePersonaLayerRoomExcel : ExcelResource
{
    [JsonProperty("CBCHIHEOEGK")] public int LayerID { get; set; }
    [JsonProperty("EEPIDJJJMAH")] public int RoomIndex { get; set; }
    [JsonProperty("BKHDBIFFIKP")] public int PresetID { get; set; }

    public override int GetId()
    {
        return LayerID * 100 + RoomIndex;
    }

    public override void Loaded()
    {
        if (!GameData.RoguePersonaLayerRoomData.TryGetValue(LayerID, out var rooms))
        {
            rooms = [];
            GameData.RoguePersonaLayerRoomData[LayerID] = rooms;
        }

        if (rooms.Any(room => room.RoomIndex == RoomIndex)) return;
        rooms.Add(this);
        rooms.Sort((a, b) => a.RoomIndex.CompareTo(b.RoomIndex));
    }
}
