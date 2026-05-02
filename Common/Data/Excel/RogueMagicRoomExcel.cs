using CastoriceHoney.Server.Enums.RogueMagic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CastoriceHoney.Server.Data.Excel;

[ResourceEntity("RogueMagicRoom.json")]
public class RogueMagicRoomExcel : ExcelResource
{
    public int RogueRoomID { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public RogueMagicRoomTypeEnum RogueRoomType { get; set; }


    public override int GetId()
    {
        return RogueRoomID;
    }

    public override void Loaded()
    {
        GameData.RogueMagicRoomData.Add(RogueRoomID, this);
    }
}