using MemoryPack;
using Newtonsoft.Json;
namespace March7thHoney.Data.Excel;

[ResourceEntity("GridFightRoleBasicInfo.json")]
[MemoryPackable]
public partial class GridFightRoleBasicInfoExcel : ExcelResource
{
    public uint ID { get; set; }
    public uint SpecialAvatarID { get; set; }
    public uint AvatarID { get; set; }
    public uint SeasonID { get; set; }
    public uint Rarity { get; set; }
    public string FrontBackType { get; set; } = "Front";
    public uint EquipmentID { get; set; }
    public List<uint> TraitList { get; set; } = [];
    public List<string> RoleSavedValueList { get; set; } = [];
    public bool IsInPool { get; set; }
    public bool IsInBook { get; set; }

    // Set by the split-table loader. Current clients resolve role skills from
    // GridFightRoleStar rather than the removed RoleSavedValueList column.
    [JsonIgnore] public bool UsesNativeSkillOverrides { get; set; }
    public uint IsFakeAvatar { get; set; }

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightRoleBasicInfoData.TryAdd(ID, this);
    }
}
