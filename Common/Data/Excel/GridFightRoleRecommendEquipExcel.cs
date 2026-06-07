using MemoryPack;
namespace March7thHoney.Data.Excel;

[ResourceEntity("GridFightRoleRecommendEquip.json")]
[MemoryPackable]
public partial class GridFightRoleRecommendEquipExcel : ExcelResource
{
    public uint RoleID { get; set; }
    public List<uint> FirstRecommendEquipList { get; set; } = [];
    public List<uint> SecondRecommendEquipList { get; set; } = [];

    public override int GetId()
    {
        return (int)RoleID;
    }

    public override void Loaded()
    {
        GameData.GridFightRoleRecommendEquipData.TryAdd(RoleID, this);
    }
}
