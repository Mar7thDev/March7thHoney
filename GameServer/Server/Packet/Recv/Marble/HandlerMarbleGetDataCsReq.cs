using CastoriceHoney.Server.GameServer.Server.Packet.Send.Marble;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Marble;

[Opcode(CmdIds.MarbleGetDataCsReq)]
public class HandlerMarbleGetDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketMarbleGetDataScRsp());
    }
}