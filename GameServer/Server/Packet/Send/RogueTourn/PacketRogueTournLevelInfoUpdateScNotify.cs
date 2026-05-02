using March7thHoney.GameServer.Game.RogueTourn;
using March7thHoney.GameServer.Game.RogueTourn.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueTourn;

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