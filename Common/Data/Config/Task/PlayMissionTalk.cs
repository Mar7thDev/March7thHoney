namespace March7thHoney.Data.Config.Task;

public class PlayMissionTalk : TaskConfigInfo
{
    public List<SimpleTalkInfo> SimpleTalkList { get; set; } = [];
}

public class SimpleTalkInfo
{
    public int TalkSentenceID { get; set; }
    public int ProtectTime { get; set; }
    public int ForceToNextTime { get; set; }
}
