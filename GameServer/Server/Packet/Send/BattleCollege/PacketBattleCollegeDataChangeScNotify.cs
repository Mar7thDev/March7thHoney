using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.BattleCollege;

public class PacketBattleCollegeDataChangeScNotify : BasePacket
{
    public PacketBattleCollegeDataChangeScNotify(PlayerInstance player) : base(5794)
    {
        var proto = new SyncBattleCollegeDataChangeScNotify();

        foreach (var id in player.BattleCollegeData?.FinishedCollegeIdList ?? [])
            proto.FinishedCollegeIdList.Add((uint)id);

        SetData(proto);
    }
}

