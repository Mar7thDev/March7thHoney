using CastoriceHoney.Server.GameServer.Server.Packet.Send.Message;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Message;

[Opcode(CmdIds.FinishSectionIdCsReq)]
public class HandlerFinishSectionIdCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = FinishSectionIdCsReq.Parser.ParseFrom(data);

        await connection.Player!.MessageManager!.FinishSection((int)req.SectionId);

        await connection.SendPacket(new PacketFinishSectionIdScRsp(req.SectionId));
    }
}