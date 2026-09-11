using MemoryPack;
using March7thHoney.Enums.Activity;
using March7thHoney.Util;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("FinalityBattleLevel.json")]
[MemoryPackable]
public partial class FinalityBattleLevelExcel : ExcelResource
{
    public int ID { get; set; }

    [JsonConverter(typeof(SafeStringEnumConverter<FinalityBattleDifficultyLevel>))]
    public FinalityBattleDifficultyLevel DifficultyLevel { get; set; }

    public int StageID { get; set; }
    public int UnlockQuest { get; set; }
    public int BattleAreaID { get; set; }
    public List<int> BattleTargetList { get; set; } = [];
    public List<int> SupportingRoleList { get; set; } = [];
    public List<int> SupportingAllRoleList { get; set; } = [];

    public override int GetId()
    {
        return ID;
    }

    public override void Loaded()
    {
        GameData.FinalityBattleLevelData.TryAdd(ID, this);
    }
}
