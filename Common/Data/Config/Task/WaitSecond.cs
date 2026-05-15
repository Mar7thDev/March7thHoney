namespace March7thHoney.Data.Config.Task;

public class WaitSecond : TaskConfigInfo
{
    public DynamicFloat WaitTime { get; set; } = new();
}
