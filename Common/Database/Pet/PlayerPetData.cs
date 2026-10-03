using MemoryPack;

namespace March7thHoney.Database.Pet;

/// <summary>
///     4.6 盲盒宠物的实例与编队。客户端用 uint64 unique id 指代每一只宠物实例并在编队里跨会话引用，
///     所以实例 id 必须持久化。宠物物品本身仍作为材料堆叠存放在背包，实例列表按材料数量对账得出。
///     新表：旧数据库首次启动时自动建空表，旧玩家读出来就是空数据。
/// </summary>
[DbTable("PlayerPet")]
public class PlayerPetData : BaseDatabaseDataHelper
{
    /// <summary>每一只拥有的盲盒宠物实例，按获得顺序。</summary>
    public List<PetInstanceData> Instances { get; set; } = [];

    /// <summary>盲盒宠物编队（随行小队），最多 3 个槽位。</summary>
    public List<PetLineupSlotData> Lineup { get; set; } = [];

    /// <summary>
    ///     当前召唤状态（EHDNPMEKPCA.LLGIIPALELD 枚举值）。-1 表示从未召唤或收回过，此时按是否拥有实例推导
    ///     （有实例为 3，否则为 0，与官服新号 / 有宠物账号的抓包一致）。
    /// </summary>
    public int CurPetRefType { get; set; } = -1;

    /// <summary>实例序号，只增不减，保证 unique id 永不复用。</summary>
    public ulong NextSerial { get; set; }
}

[MemoryPackable]
public partial class PetInstanceData
{
    public ulong UniqueId { get; set; }
    public int PetId { get; set; }
    public int ChromaLevel { get; set; }
    public bool ChromaActive { get; set; }
}

[MemoryPackable]
public partial class PetLineupSlotData
{
    public ulong UniqueId { get; set; }
    public int Slot { get; set; }
}
