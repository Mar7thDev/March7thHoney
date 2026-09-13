using MemoryPack;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[MemoryPackable]
public partial class MechCraftCell
{
    [JsonProperty("GAOAMKBNDPN")] public int X { get; set; }
    [JsonProperty("EBJKJAGJGPK")] public int Y { get; set; }
}

[MemoryPackable]
public partial class MechCraftChipCount
{
    [JsonProperty("PFDHNIJBGKO")] public int ChipID { get; set; }
    [JsonProperty("HCDEOKNIGKG")] public int Count { get; set; }
}
