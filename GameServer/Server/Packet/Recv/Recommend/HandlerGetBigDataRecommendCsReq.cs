using CastoriceHoney.Server.GameServer.Server.Packet.Send.Recommend;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Recommend;

[Opcode(CmdIds.GetBigDataRecommendCsReq)]
public class HandlerGetBigDataRecommendCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetBigDataRecommendCsReq.Parser.ParseFrom(data);
        await connection.SendPacket(new PacketGetBigDataRecommendScRsp(req.EquipAvatar, req.BigDataRecommendType));
    }
}