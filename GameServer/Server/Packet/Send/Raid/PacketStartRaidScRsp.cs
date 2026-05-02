using CastoriceHoney.Server.Database.Scene;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Raid;

public class PacketStartRaidScRsp : BasePacket
{
    public PacketStartRaidScRsp(RaidRecord record, PlayerInstance player) : base(CmdIds.StartRaidScRsp)
    {
        var proto = new StartRaidScRsp();

        SetData(proto);
    }

    public PacketStartRaidScRsp(Retcode ret) : base(CmdIds.StartRaidScRsp)
    {
        var proto = new StartRaidScRsp
        {
            Retcode = (uint)ret
        };

        SetData(proto);
    }
}

