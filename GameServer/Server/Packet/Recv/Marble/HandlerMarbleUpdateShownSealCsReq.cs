using CastoriceHoney.Server.GameServer.Server.Packet.Send.Marble;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Marble;

[Opcode(CmdIds.MarbleUpdateShownSealCsReq)]
public class HandlerMarbleUpdateShownSealCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = MarbleUpdateShownSealCsReq.Parser.ParseFrom(data);

        await connection.SendPacket(new PacketMarbleUpdateShownSealScRsp(req.UpdateSealList));
    }
}