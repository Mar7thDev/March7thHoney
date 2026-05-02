using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Lineup;

public class PacketGetCurLineupDataScRsp : BasePacket
{
    public PacketGetCurLineupDataScRsp(PlayerInstance player) : base(CmdIds.GetCurLineupDataScRsp)
    {
        var data = new GetCurLineupDataScRsp
        {
            Lineup = player.LineupManager?.GetCurLineup()?.ToProto() ?? new LineupInfo()
        };

        SetData(data);
    }
}