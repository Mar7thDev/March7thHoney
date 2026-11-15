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

    // Season 2 pseudo-roles (604202) that never act as a battle avatar.
    public bool IsFakeAvatar { get; set; }

    // Set by the split-table loader (4.7 beta GridFightCampSeason). Current clients resolve role skills
    // from GridFightRoleStar rather than the removed RoleSavedValueList column. Appended after
    // IsFakeAvatar so existing season-2 caches keep their MemoryPack layout.
    [JsonIgnore] public bool UsesNativeSkillOverrides { get; set; }

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightRoleBasicInfoData.TryAdd(ID, this);
    }
}
