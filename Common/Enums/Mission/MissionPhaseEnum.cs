using March7thHoney.Enums.Task;
using March7thHoney.Proto;

namespace March7thHoney.Enums.Mission;

public enum MissionPhaseEnum
{
    Accept = 0,
    Finish = 1,
    None = 3,
    Cancel = 4,

    /// <summary>
    ///     Chain position reached, but a PlayerLevel/WorldLevel gate still blocks the start.
    ///     Stored in <c>MissionData.PreparedMainMissionIds</c>, not in the running list.
    /// </summary>
    Prepared = 5
}

public static class MissionStatusExtensions
{
    public static MissionStatus ToProto(this MissionPhaseEnum status)
    {
        return status switch
        {
            MissionPhaseEnum.None => MissionStatus.MissionNone,
            MissionPhaseEnum.Accept => MissionStatus.MissionDoing,
            MissionPhaseEnum.Finish => MissionStatus.MissionFinish,
            MissionPhaseEnum.Cancel => MissionStatus.MissionNone,
            MissionPhaseEnum.Prepared => MissionStatus.MissionPrepared,
            _ => MissionStatus.MissionNone
        };
    }

    public static SubMissionStateEnum ToStateEnum(this MissionPhaseEnum status)
    {
        return status switch
        {
            MissionPhaseEnum.None => SubMissionStateEnum.Unknow,
            MissionPhaseEnum.Accept => SubMissionStateEnum.Started,
            MissionPhaseEnum.Finish => SubMissionStateEnum.Finish,
            MissionPhaseEnum.Cancel => SubMissionStateEnum.TakenAndNotStarted,
            _ => SubMissionStateEnum.Unknow
        };
    }
}