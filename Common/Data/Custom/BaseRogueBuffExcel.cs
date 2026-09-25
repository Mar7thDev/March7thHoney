using MemoryPack;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Rogue;
using March7thHoney.Proto;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Data.Custom;

// RogueBuffData mixes classic and Divergent Universe rows; the union keeps the subtype through Resource.bin,
// otherwise every cached row comes back as the base type and `is RogueBuffExcel` filters match nothing.
[MemoryPackable]
[MemoryPackUnion(0, typeof(RogueBuffExcel))]
[MemoryPackUnion(1, typeof(RogueTournBuffExcel))]
public abstract partial class BaseRogueBuffExcel : ExcelResource
{
    public int MazeBuffID { get; set; }
    public int MazeBuffLevel { get; set; }
    public int RogueBuffType { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public RogueBuffCategoryEnum RogueBuffCategory { get; set; }

    public int RogueBuffTag { get; set; }

    public override int GetId()
    {
        return MazeBuffID * 100 + MazeBuffLevel;
    }

    // TODO 4.3: HJGFDIKDIHO 在 4.3 中已消失或更名，RogueBuff 不在登录路径，暂时移除
}
