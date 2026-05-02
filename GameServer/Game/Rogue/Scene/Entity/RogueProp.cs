using CastoriceHoney.Server.Data.Config.Scene;
using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.Enums.RogueMagic;
using CastoriceHoney.Server.Enums.TournRogue;
using CastoriceHoney.Server.GameServer.Game.Scene;
using CastoriceHoney.Server.GameServer.Game.Scene.Entity;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.Rogue.Scene.Entity;

public class RogueProp(SceneInstance scene, MazePropExcel excel, GroupInfo group, PropInfo prop)
    : EntityProp(scene, excel, group, prop)
{
    public RogueProp(EntityProp prop) : this(prop.Scene, prop.Excel, prop.Group, prop.PropInfo)
    {
    }

    public int NextRoomId { get; set; }
    public int NextSiteId { get; set; }
    public int ChestCanUseTimes { get; set; }
    public int CustomPropId { get; set; }

    public bool IsChessRogue { get; set; } = false;
    public bool IsLastRoom { get; set; } = false;

    public bool IsTournRogue { get; set; } = false;
    public bool EnterNextLayer { get; set; } = false;
    public RogueTournRoomTypeEnum RoomType { get; set; } = RogueTournRoomTypeEnum.Unknown;

    public bool IsMagicRogue { get; set; } = false;
    public RogueMagicRoomTypeEnum MagicRoomType { get; set; } = RogueMagicRoomTypeEnum.Unknown;

    public override SceneEntityInfo ToProto()
    {
        var proto = base.ToProto();

        if (NextRoomId != 0 || NextSiteId != 0 || ChestCanUseTimes != 0) // do not set if all are 0
            proto.Prop.ExtraInfo = new PropExtraInfo
            {
                RogueGameInfo = new PropRogueInfo
                {
                    RoomId = (uint)NextRoomId,
                    SiteId = (uint)NextSiteId
                }
            };

        if (IsChessRogue)
            proto.Prop.ExtraInfo = new PropExtraInfo
            {
                ChessRogueInfo = new PropChessRogueInfo
                {
                    EnterNextCell = !IsLastRoom
                }
            };

        if (IsTournRogue)
            proto.Prop.ExtraInfo = new PropExtraInfo
            {
                RogueTournDoorInfo = new RogueTournDoorInfo
                {
                    EnterNextLayer = EnterNextLayer,
                    RogueDoorNextRoomType = (uint)RoomType
                }
            };

        if (IsMagicRogue)
            proto.Prop.ExtraInfo = new PropExtraInfo
            {
                RogueMagicDoorInfo = new RogueMagicDoorInfo
                {
                    EnterNextLayer = EnterNextLayer,
                    RogueDoorNextRoomType = (uint)MagicRoomType
                }
            };

        if (CustomPropId != 0) proto.Prop.PropId = (uint)CustomPropId;

        return proto;
    }
}