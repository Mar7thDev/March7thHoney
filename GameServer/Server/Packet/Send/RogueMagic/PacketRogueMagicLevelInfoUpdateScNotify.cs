using CastoriceHoney.Server.GameServer.Game.RogueMagic;
using CastoriceHoney.Server.GameServer.Game.RogueMagic.Scene;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueMagic;

public class PacketRogueMagicLevelInfoUpdateScNotify : BasePacket
{
    public PacketRogueMagicLevelInfoUpdateScNotify(RogueMagicInstance instance, List<RogueMagicLevelInstance> levels,
        List<int>? updateRoomIndexList = null) : base(CmdIds.RogueMagicLevelInfoUpdateScNotify)
    {
        var proto = new RogueMagicLevelInfoUpdateScNotify
        {
            Status = instance.LevelStatus,
            CurLevelIndex = (uint)(instance.CurLevel?.LevelIndex ?? 0),
            LevelInfoList = { levels.Select(x => x.ToProto(updateRoomIndexList)) }
        };

        SetData(proto);
    }
}