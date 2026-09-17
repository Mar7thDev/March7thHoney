using MemoryPack;
namespace March7thHoney.Data.Excel;

[ResourceEntity("TutorialData.json")]
[MemoryPackable]
public partial class TutorialDataExcel : ExcelResource
{
    public int TutorialID { get; set; }
    public int Priority { get; set; }

    /// <summary>"AutoFinish" marks a self-closing tutorial (safe to open even with no finish condition).</summary>
    public string RestoreType { get; set; } = "";

    public bool CanInterrupt { get; set; }
    public List<TutorialTriggerParam> TriggerParams { get; set; } = [];

    /// <summary>What closes the tutorial once open; evaluated by TutorialHelper.CanClose before an unlock is granted.</summary>
    public List<TutorialTriggerParam> FinishTriggerParams { get; set; } = [];

    public override int GetId()
    {
        return TutorialID;
    }

    public override void Loaded()
    {
        GameData.TutorialDataData.TryAdd(TutorialID, this);
    }
}

/// <summary>One trigger/finish condition row of TutorialData.json (keys TriggerType / TriggerParam).</summary>
[MemoryPackable]
public partial class TutorialTriggerParam
{
    public string TriggerType { get; set; } = "";
    public string TriggerParam { get; set; } = "";
}
