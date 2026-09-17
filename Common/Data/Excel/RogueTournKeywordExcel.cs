using MemoryPack;

namespace March7thHoney.Data.Excel;

[ResourceEntity("RogueTournKeyword.json")]
[MemoryPackable]
public partial class RogueTournKeywordExcel : ExcelResource
{
    public int KeywordID { get; set; }

    public override int GetId()
    {
        return KeywordID;
    }

    public override void Loaded()
    {
        GameData.RogueTournKeywordData.TryAdd(KeywordID, this);
    }
}
