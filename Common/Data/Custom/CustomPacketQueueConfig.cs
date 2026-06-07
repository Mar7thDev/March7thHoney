using MemoryPack;
using March7thHoney.Enums.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Data.Custom;

[MemoryPackable]
public partial class CustomPacketQueueConfig
{
    public List<PacketActionData> Queue { get; set; } = [];
}

[MemoryPackable]
public partial class PacketActionData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public PacketActionTypeEnum Action { get; set; }
    public PacketActionParamData Param { get; set; } = new();
}

[MemoryPackable]
public partial class PacketActionParamData
{
    public string PacketName { get; set; } = "";
    public string PacketData { get; set; } = "";
    public bool InterruptFormalHandler { get; set; } = false;
}
