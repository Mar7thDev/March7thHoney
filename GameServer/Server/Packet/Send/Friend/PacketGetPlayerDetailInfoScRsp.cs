using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;

public class PacketGetPlayerDetailInfoScRsp : BasePacket
{
    public PacketGetPlayerDetailInfoScRsp(PlayerDetailInfo info) : base(CmdIds.GetPlayerDetailInfoScRsp)
    {
        var proto = new GetPlayerDetailInfoScRsp
        {
            DetailInfo = info
        };

        SetData(proto);
    }

    public PacketGetPlayerDetailInfoScRsp() : base(CmdIds.GetPlayerDetailInfoScRsp)
    {
        var proto = new GetPlayerDetailInfoScRsp
        {
            Retcode = 3612
        };

        SetData(proto);
    }
}