using MemoryPack;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

/// <summary>
///     One Persona gift: the id space of the cleared-room gift offer (KBEBGLAAEGM) and its answer (JGOLAJCHKAG).
///     StyleIDList names the RoguePersonaStyle ids the gift belongs to (empty = not style-bound); IsCore marks the
///     Core band, which official offers never mix with the other tiers.
/// </summary>
[ResourceEntity("RoguePersonaStyleGift.json")]
[MemoryPackable]
public partial class RoguePersonaStyleGiftExcel : ExcelResource
{
    [JsonProperty("FMDMDDCBPAM")] public int GiftID { get; set; }
    [JsonProperty("PMIEAEGJNMJ")] public string Rarity { get; set; } = "";
    [JsonProperty("NIKKAPEIDJO")] public List<int> StyleIDList { get; set; } = [];
    [JsonProperty("HILFNIOLPHN")] public bool IsCore { get; set; }

    public override int GetId()
    {
        return GiftID;
    }

    public override void Loaded()
    {
        GameData.RoguePersonaStyleGiftData.TryAdd(GiftID, this);
    }
}
