using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;
using CastoriceHoney.Server.Util;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Fight;

public class PacketFightHeartBeatScRsp : BasePacket
{
    public PacketFightHeartBeatScRsp(ulong clientTime) : base(CmdIds.FightHeartBeatScRsp)
    {
        var proto = new FightHeartBeatScRsp
        {
            ServerTimeMs = (ulong)Extensions.GetUnixMs(),
            ClientTimeMs = clientTime
        };

        SetData(proto);
    }
}