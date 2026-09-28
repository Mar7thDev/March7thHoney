using MemoryPack;
using March7thHoney.Data.Custom;
using March7thHoney.Util;

namespace March7thHoney.Data.Excel;

[ResourceEntity("RogueTournBuffGroup.json")]
[MemoryPackable]
public partial class RogueTournBuffGroupExcel : BaseRogueBuffGroupExcel
{
    public int RogueBuffGroupID { get; set; }
    public List<int> RogueBuffDrop { get; set; } = [];

    public override void Loaded()
    {
        GroupId = RogueBuffGroupID;
        GameData.RogueBuffGroupData.Add(GetId(), this);
        LoadBuff();
    }

    public override void AfterAllDone()
    {
        LoadBuff();
    }

    public void LoadBuff()
    {
        if (IsLoaded) return;
        var count = 0;
        List<RogueTournBuffExcel> buffs = [.. GameData.RogueBuffData.Values.OfType<RogueTournBuffExcel>()];
        foreach (var buffId in RogueBuffDrop)
        {
            if (buffs.FirstOrDefault(x => x.RogueBuffTag == buffId) is { } buff)
            {
                BuffList.SafeAdd(buff);
                count++;
            }
            else
            {
                // might is group id
                if (!GameData.RogueBuffGroupData.TryGetValue(buffId, out var group)) continue;
                if (group is not RogueTournBuffGroupExcel e) continue;
                e.LoadBuff();
                BuffList.SafeAddRange(e.BuffList);
                // A child still missing rows (not loaded yet) must leave this group open for AfterAllDone.
                if (e.IsLoaded) count++;
            }
        }

        if (count == RogueBuffDrop.Count) IsLoaded = true;
    }
}
