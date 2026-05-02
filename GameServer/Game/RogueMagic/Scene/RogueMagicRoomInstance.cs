using March7thHoney.Data;
using March7thHoney.Data.Custom;
using March7thHoney.Enums.RogueMagic;
using March7thHoney.GameServer.Game.RogueMagic.Adventure;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.RogueMagic.Scene;

public class RogueMagicRoomInstance(int roomIndex, RogueMagicLevelInstance levelInstance)
{
    public int RoomId { get; set; }
    public int RoomIndex { get; set; } = roomIndex;
    public RogueMagicRoomStatus Status { get; set; } = RogueMagicRoomStatus.None;
    public RogueMagicLevelInstance LevelInstance { get; set; } = levelInstance;
    public RogueMagicRoomTypeEnum RoomType { get; set; }

    public RogueMagicRoomConfig? Config { get; set; }

    public RogueMagicAdventureInstance? AdventureInstance { get; set; }

    public RogueMagicRoomInfo ToProto()
    {
        return new RogueMagicRoomInfo
        {
            RoomId = (uint)RoomId,
            RoomIndex = (uint)RoomIndex,
            Status = Status
        };
    }

    public void Init(RogueMagicRoomTypeEnum type)
    {
        if (Status == RogueMagicRoomStatus.Processing || Status == RogueMagicRoomStatus.Finish)
            return; // already initialized

        RoomType = type;
        Status = RogueMagicRoomStatus.Processing;
        // get config
        Config = RoomType == RogueMagicRoomTypeEnum.Adventure
            ? GameData.RogueMagicRoomGenData.Where(x => x.RoomType == RoomType).ToList().RandomElement()
            : GameData.RogueMagicRoomGenData
                .Where(x => x.EntranceId == LevelInstance.EntranceId && x.RoomType == RoomType).ToList()
                .RandomElement();

        if (Config == null)
        {
            Status = RogueMagicRoomStatus.Finish;
            return;
        }

        RoomId = GameData.RogueMagicRoomData.Where(x => x.Value.RogueRoomType == RoomType).Select(x => x.Key).ToList()
            .RandomElement();

        if (RoomType == RogueMagicRoomTypeEnum.Adventure)
        {
            AdventureInstance = new RogueMagicAdventureInstance(GameData.RogueMagicAdventureRoomData.Values
                .Where(x => x.AdventureType == Config.AdventureType).ToList().RandomElement());

            RoomId = AdventureInstance.Excel.RoomID;
        }
    }

    public List<int> GetLoadGroupList()
    {
        var groupList = new List<int>();
        groupList.AddRange(Config!.DefaultLoadBasicGroup);
        groupList.AddRange(Config.DefaultLoadGroup);

        //if (RoomIndex == 1)  // first room
        groupList.AddRange(Config.SubMonsterGroup);

        return groupList;
    }
}