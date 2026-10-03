using MemoryPack;

namespace March7thHoney.Data.Excel;

/// <summary>The Divergent Universe level ladder: (MainTournID, Level) → exp threshold and the RewardData id it pays.</summary>
[ResourceEntity("RogueTournExpReward.json")]
[MemoryPackable]
public partial class RogueTournExpRewardExcel : ExcelResource
{
    public int MainTournID { get; set; }
    public int Level { get; set; }
    public int Exp { get; set; }
    public int RewardID { get; set; }

    public override int GetId()
    {
        return MainTournID << 16 | Level;
    }

    public override void Loaded()
    {
        GameData.RogueTournExpRewardData.TryAdd(GetId(), this);
    }
}
