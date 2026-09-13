using MemoryPack;
using March7thHoney.Enums.Activity;
using March7thHoney.Util;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("ExpeditionBattleFunTitle.json")]
[MemoryPackable]
public partial class ExpeditionBattleFunTitleExcel : ExcelResource
{
    public int ID { get; set; }

    [JsonConverter(typeof(SafeStringEnumConverter<ExpeditionBattleFunTitleType>))]
    public ExpeditionBattleFunTitleType Type { get; set; }

    [JsonConverter(typeof(SafeStringEnumConverter<ExpeditionBattleFunConditionType>))]
    public ExpeditionBattleFunConditionType ConditionParamType { get; set; }

    public ulong ConditionParam { get; set; }

    public override int GetId()
    {
        return ID;
    }

    public override void Loaded()
    {
        GameData.ExpeditionBattleFunTitleData.TryAdd(ID, this);
    }
}
