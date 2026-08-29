using MemoryPack;
using Newtonsoft.Json;

namespace March7thHoney.Data.Custom;

[MemoryPackable]
public partial class VideoKeysConfig
{
    // The JSON keys are the official GetVideoVersionKeyScRsp list names. They used to be spelled
    // "ActivityVideoKeyData" / "videoKeyInfoList", so the second list never matched its property and
    // silently loaded as empty - and the two lists were swapped on top of that (the ids the official
    // server reports under video_key_info_list sat in the activity list and vice versa).
    [JsonProperty("activityVideoKeyInfoList")]
    public List<ActivityVideoKeyInfoList> ActivityVideoKeyData { get; set; } = new();

    [JsonProperty("videoKeyInfoList")]
    public List<VideoKeyInfoList> VideoKeyInfoData { get; set; } = new();

    public int TotalCount => ActivityVideoKeyData.Count + VideoKeyInfoData.Count;
}

[MemoryPackable]
public partial class ActivityVideoKeyInfoList
{
    public int Id { get; set; }
    public ulong VideoKey { get; set; }

    // Appended after the pre-existing members so an older Resource.bin still deserializes.
    public ulong IBDIMHPOPOJ { get; set; }

    [JsonProperty("psField3")] public ulong PsField3 { get; set; }
}

[MemoryPackable]
public partial class VideoKeyInfoList
{
    public int Id { get; set; }
    public ulong VideoKey { get; set; }

    public ulong IBDIMHPOPOJ { get; set; }

    [JsonProperty("psField3")] public ulong PsField3 { get; set; }
}
