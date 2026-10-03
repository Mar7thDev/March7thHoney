using MemoryPack;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

/// <summary>
///     The room card a Persona preset places: a composition type at a level (1001 → Battle lv3, 1002 → Boss lv1,
///     1003 → Respite, 1004 → Conversion).
/// </summary>
[ResourceEntity("RoguePersonaRoomPreset.json")]
[MemoryPackable]
public partial class RoguePersonaRoomPresetExcel : ExcelResource
{
    [JsonProperty("LIIPLGLNPGB")] public int PresetID { get; set; }
    [JsonProperty("LLICIMBCNPF")] public int CompositionType { get; set; }
    [JsonProperty("AAGKEBFHLMC")] public int Level { get; set; }
    [JsonProperty("FJIKMHCJMKH")] public List<int> AttributeList { get; set; } = [];

    public override int GetId()
    {
        return PresetID;
    }

    public override void Loaded()
    {
        GameData.RoguePersonaRoomPresetData.TryAdd(PresetID, this);
    }
}
