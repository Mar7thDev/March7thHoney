namespace March7thHoney.Data.Config;

public class MissionPartialLayout
{
    public List<MissionPartialLayoutItem> BakeInfoLayouts { get; set; } = [];
}

public class MissionPartialLayoutItem
{
    public string UniqueName { get; set; } = string.Empty;
    public string TypeId { get; set; } = string.Empty;
    public int Offset { get; set; }
}
