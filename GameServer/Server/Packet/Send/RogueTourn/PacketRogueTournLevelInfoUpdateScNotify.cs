using CastoriceHoney.Server.GameServer.Game.RogueTourn;
using CastoriceHoney.Server.GameServer.Game.RogueTourn.Scene;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueTourn;

public class PacketRogueTournLevelInfoUpdateScNotify : BasePacket
{
    public PacketRogueTournLevelInfoUpdateScNotify(RogueTournInstance instance, List<RogueTournLevelInstance> levels) :
        base(CmdIds.RogueTournLevelInfoUpdateScNotify)
    {
        var proto = new RogueTournLevelInfoUpdateScNotify
        {
            Status = instance.LevelStatus,
            CurLevelIndex = (uint)(instance.CurLevel?.LevelIndex ?? 0),
            LevelInfoList = { levels.Select(x => x.ToProto()) }
        };

        SetData(proto);
    }
}