using CastoriceHoney.Server.Enums.Rogue;
using CastoriceHoney.Server.Proto;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CastoriceHoney.Server.Data.Custom;

public class BaseRogueBuffExcel : ExcelResource
{
    public int MazeBuffID { get; set; }
    public int MazeBuffLevel { get; set; }
    public int RogueBuffType { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public RogueBuffCategoryEnum RogueBuffCategory { get; set; }

    public int RogueBuffTag { get; set; }

    public override int GetId()
    {
        return MazeBuffID * 100 + MazeBuffLevel;
    }

    public RogueCommonBuff ToProto()
    {
        return new RogueCommonBuff
        {
            BuffId = (uint)MazeBuffID,
            BuffLevel = (uint)MazeBuffLevel
        };
    }
}