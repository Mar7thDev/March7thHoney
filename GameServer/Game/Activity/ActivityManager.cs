using March7thHoney.Data;
using March7thHoney.Database;
using March7thHoney.Database.Activity;
using March7thHoney.GameServer.Game.Activity.Activities;
// TODO 4.3: DiceCombat 模块暂停编译
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Activity;

public class ActivityManager : BasePlayerManager
{
    public ActivityManager(PlayerInstance player) : base(player)
    {
        Data = DatabaseHelper.Instance!.GetInstanceOrCreateNew<ActivityData>(player.Uid);

        if (Data.TrialActivityData.CurTrialStageId != 0) TrialActivityInstance = new TrialActivityInstance(this);

        // TODO 4.3: DiceCombat 初始化暂停
    }

    #region Data

    public ActivityData Data { get; set; }

    #endregion

    #region Instance

    public TrialActivityInstance? TrialActivityInstance { get; set; }
    // TODO 4.3: DiceCombat 暂停

    #endregion

    public List<ActivityScheduleData> ToProto()
    {
        var proto = new List<ActivityScheduleData>();

        foreach (var activity in GameData.ActivityConfig.ScheduleData)
            proto.Add(new ActivityScheduleData
            {
                ActivityId = (uint)activity.ActivityId,
                BeginTime = activity.BeginTime,
                EndTime = activity.EndTime,
                PanelId = (uint)activity.PanelId
            });

        return proto;
    }
}
