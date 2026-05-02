using March7thHoney.GameServer.Server.Packet.Send.Marble;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Marble;

[Opcode(CmdIds.MarbleGetDataCsReq)]
public class HandlerMarbleGetDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketMarbleGetDataScRsp());
    }
}