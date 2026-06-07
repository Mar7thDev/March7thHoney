using MemoryPack;
namespace March7thHoney.Data.Custom;

[MemoryPackable]
public partial class QueryProductInfoConfig
{
    public List<QueryProductInfoItem> ProductList { get; set; } = [];
}

[MemoryPackable]
public partial class QueryProductInfoItem
{
    public uint JCJGHCOEOOJ { get; set; }
    public uint MEMNCJLKAEE { get; set; }
    public uint EEFHEBKHKAB { get; set; }
    public long BeginTime { get; set; }
    public long EndTime { get; set; }
    [MemoryPackIgnore] public object GiftType { get; set; } = "";
    public string PriceTier { get; set; } = "";
    public string ProductId { get; set; } = "";
    public bool DoubleReward { get; set; }
}
