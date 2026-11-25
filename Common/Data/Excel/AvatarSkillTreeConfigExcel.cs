using MemoryPack;
using March7thHoney.Data.Config;

namespace March7thHoney.Data.Excel;

// The story-variant trial bases (7xxx, e.g. Imbibitor Lunae 7213 behind trial 1021213) keep their skill
// trees only in the Trial table; without it those battle avatars went out with an empty skilltree_list
// (official 4.7 sends points 7213001-7213202). Its point IDs do not overlap the main tables.
[ResourceEntity("AvatarSkillTreeConfig.json,AvatarSkillTreeConfigLD.json,AvatarSkillTreeConfigTrial.json", true,
    OptionalFileNames = "AvatarSkillTreeConfigTrial.json")]
[MemoryPackable]
public partial class AvatarSkillTreeConfigExcel : ExcelResource
{
    public int PointID { get; set; }
    public int Level { get; set; }
    public int AvatarID { get; set; }
    public string AnchorType { get; set; } = "";
    public int EnhancedID { get; set; }
    public bool DefaultUnlock { get; set; }
    public int MaxLevel { get; set; }
    public List<FixedValueInfo<double>> ParamList { get; set; } = [];

    public int GetMultiPointId()
    {
        if (string.IsNullOrWhiteSpace(AnchorType)) return 0;

        var result = 0;
        var factor = 1;
        for (var i = AnchorType.Length - 1; i >= 0; i--)
        {
            var ch = AnchorType[i];
            if (ch is < '0' or > '9') break;
            result += (ch - '0') * factor;
            factor *= 10;
        }

        return result;
    }

    public override int GetId()
    {
        return PointID * 100 + Level;
    }

    public override void AfterAllDone()
    {
        GameData.AvatarConfigData.TryGetValue(AvatarID, out var excel);
        GameData.AvatarSkillTreeConfigData.TryAdd(GetId(), this);
        if (excel == null) return;

        excel.DefaultSkillTree.TryAdd(EnhancedID, []);
        excel.SkillTree.TryAdd(EnhancedID, []);
        if (DefaultUnlock && excel.DefaultSkillTree[EnhancedID].All(x => x.PointID != PointID))
            excel.DefaultSkillTree[EnhancedID].Add(this);
        if (excel.SkillTree[EnhancedID].All(x => x.PointID != PointID)) excel.SkillTree[EnhancedID].Add(this);
    }
}
