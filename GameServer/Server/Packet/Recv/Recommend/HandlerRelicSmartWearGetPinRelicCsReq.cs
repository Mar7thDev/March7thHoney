using CastoriceHoney.Server.GameServer.Server.Packet.Send.Recommend;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Recommend;

[Opcode(CmdIds.RelicSmartWearGetPinRelicCsReq)]
public class HandlerRelicSmartWearGetPinRelicCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RelicSmartWearGetPinRelicCsReq.Parser.ParseFrom(data);
        await connection.SendPacket(new PacketRelicSmartWearGetPinRelicScRsp(req.AvatarId));
    }
}