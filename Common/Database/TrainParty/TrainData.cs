using March7thHoney.Data;
using March7thHoney.Proto;
using SqlSugar;

namespace March7thHoney.Database.TrainParty;

[SugarTable("TrainParty")]
public class TrainData : BaseDatabaseDataHelper
{
    public int Fund { get; set; }
    public int RecordId { get; set; }
    public uint WorldId { get; set; }
    public long RefreshTime { get; set; }
    [SugarColumn(IsJson = true)] public Dictionary<int, TrainAreaInfo> Areas { get; set; } = [];
    [SugarColumn(IsJson = true)] public List<TrainSelfDisplayEntry> SelfDisplay { get; set; } = [];
}

public class TrainAreaInfo
{
    public int AreaId { get; set; }
    public List<int> StepList { get; set; } = [];
    public Dictionary<int, int> DynamicInfo { get; set; } = [];

    public TrainPartyArea ToProto()
    {
        var info = new TrainPartyArea
        {
            AreaId = (uint)AreaId,
            AreaStepInfo = new AreaStepInfo(),
            StepIdList = { StepList.Select(x => (uint)x) },
            VerifyStepIdList = { StepList.Select(x => (uint)x) },
            Progress = 100,
            DynamicInfo =
            {
                DynamicInfo.Select(x => new AreaDynamicInfo
                {
                    DiceSlotId = (uint)x.Key,
                    DiyDynamicId = (uint)x.Value
                })
            }
        };

        foreach (var step in StepList)
        {
            GameData.TrainPartyStepConfigData.TryGetValue(step, out var stepExcel);
            if (stepExcel == null) continue;

            info.StaticPropIdList.AddRange(stepExcel.StaticPropIDList.Select(x => (uint)x));
        }

        return info;
    }
}

public class TrainSelfDisplayEntry
{
    public uint Slot { get; set; }
    public uint Id { get; set; }
    public uint Type { get; set; }
}
