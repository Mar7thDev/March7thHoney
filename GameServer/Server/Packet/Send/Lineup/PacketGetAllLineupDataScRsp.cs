using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Lineup;

public class PacketGetAllLineupDataScRsp : BasePacket
{
    public PacketGetAllLineupDataScRsp(PlayerInstance player) : base(CmdIds.GetAllLineupDataScRsp)
    {
        var proto = new GetAllLineupDataScRsp
        {
            CurIndex = (uint)player.LineupManager!.LineupData.CurLineup
        };
        foreach (var lineup in player.LineupManager.GetAllLineup()) proto.LineupList.Add(lineup.ToProto());

        SetData(proto);
    }
}