using March7thHoney.GameServer.Game.RogueMagic;
using March7thHoney.GameServer.Game.RogueMagic.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueMagic;

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