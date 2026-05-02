using March7thHoney.Enums.TournRogue;
using March7thHoney.Proto;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace March7thHoney.Data.Excel;

[ResourceEntity("RogueTournTitanBless.json")]
public class RogueTournTitanBlessExcel : ExcelResource
{
    public int TitanBlessID { get; set; }
    public int MazeBuffID { get; set; }
    public int TitanBlessLevel { get; set; }
    public int BlessRatio { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public RogueTitanTypeEnum TitanType { get; set; }

    [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
    public List<RogueTitanCategoryEnum> BlessBattleDisplayCategoryList { get; set; } = [];

    public override int GetId()
    {
        return TitanBlessID;
    }

    public override void Loaded()
    {
        GameData.RogueTournTitanBlessData.Add(TitanBlessID, this);
    }

    public RogueCommonActionResult ToResultProto(RogueCommonActionResultSourceType select)
    {
        return new RogueCommonActionResult
        {
            Source = select,
            RogueAction = new RogueCommonActionResultData
            {
                TitanBlessEvent = new RogueTitanBlessEvent
                {
                    EventUniqueId = (uint)TitanBlessID
                }
            }
        };
    }
}