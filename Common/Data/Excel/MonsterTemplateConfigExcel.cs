using MemoryPack;
namespace March7thHoney.Data.Excel;

[ResourceEntity("MonsterTemplateConfig.json")]
[MemoryPackable]
public partial class MonsterTemplateConfigExcel : ExcelResource
{
    public int MonsterTemplateID { get; set; }
    public List<int> NPCMonsterList { get; set; } = [];
    public int MonsterCampID { get; set; }

    public override int GetId()
    {
        return MonsterTemplateID;
    }

    public override void Loaded()
    {
        GameData.MonsterTemplateConfigData.Add(MonsterTemplateID, this);
    }
}
