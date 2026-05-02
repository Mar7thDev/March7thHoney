using CastoriceHoney.Server.Data.Custom;

namespace CastoriceHoney.Server.Data.Excel;

[ResourceEntity("RogueTournBuff.json")]
public class RogueTournBuffExcel : BaseRogueBuffExcel
{
    public bool IsInHandbook { get; set; }

    public override void Loaded()
    {
        GameData.RogueBuffData.TryAdd(GetId(), this);
    }
}