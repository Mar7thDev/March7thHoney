using MemoryPack;
namespace March7thHoney.Data.Config.Scene;

[MemoryPackable]
public partial class NpcInfo : PositionInfo
{
    public int NPCID { get; set; }
    public bool IsClientOnly { get; set; }
    public bool LoadOnInitial { get; set; } = true;

    // Raw adventure-AI reference, read by GroupInfo.Load; not cached.
    [MemoryPackIgnore] public NpcAIConfigInfo? AIConfig { get; set; }
}

/// <summary>The NPC's AI wiring in a group export; only the AI file path is read.</summary>
public class NpcAIConfigInfo
{
    public string AIFile { get; set; } = "";
}
