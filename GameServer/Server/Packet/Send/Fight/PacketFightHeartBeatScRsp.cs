using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Server.Packet.Send.Fight;

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