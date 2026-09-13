using MemoryPack;

namespace March7thHoney.Data.Excel;

[ResourceEntity("ConstValueClient.json")]
[MemoryPackable]
public partial class ConstValueClientExcel : ExcelResource
{
    public string ConstValueName { get; set; } = string.Empty;
    public GridFightDynamicValue Value { get; set; } = new();

    public override int GetId()
    {
        return StringComparer.Ordinal.GetHashCode(ConstValueName);
    }

    public override void Loaded()
    {
        GameData.ConstValueClientData[ConstValueName] = Value;
    }
}
