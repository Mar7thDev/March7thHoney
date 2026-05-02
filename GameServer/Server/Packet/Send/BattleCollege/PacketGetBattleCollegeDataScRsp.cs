using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.BattleCollege;

public class PacketGetBattleCollegeDataScRsp : BasePacket
{
    public PacketGetBattleCollegeDataScRsp(PlayerInstance player) : base(CmdIds.GetBattleCollegeDataScRsp)
    {
        var proto = new GetBattleCollegeDataScRsp();

        foreach (var id in player.BattleCollegeData?.FinishedCollegeIdList ?? [])
            proto.FinishedCollegeIdList.Add((uint)id);

        SetData(proto);
    }
}