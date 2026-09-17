using MemoryPack;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("RoguePersonaStyle.json")]
[MemoryPackable]
public partial class RoguePersonaStyleExcel : ExcelResource
{
    [JsonProperty("KLOEJIMMPJM")] public int StyleID { get; set; }

    public override int GetId()
    {
        return StyleID;
    }

    public override void Loaded()
    {
        GameData.RoguePersonaStyleData.TryAdd(StyleID, this);
    }
}
