using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;
using CastoriceHoney.Server.Util;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Player;

public class PacketPlayerHeartBeatScRsp : BasePacket
{
    public PacketPlayerHeartBeatScRsp(long clientTime) : base(CmdIds.PlayerHeartBeatScRsp)
    {
        var data = new PlayerHeartBeatScRsp
        {
            ClientTimeMs = (ulong)clientTime,
            ServerTimeMs = (ulong)Extensions.GetUnixMs()
        };

        SetData(data);
    }
}