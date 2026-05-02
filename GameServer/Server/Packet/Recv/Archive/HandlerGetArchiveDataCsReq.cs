using CastoriceHoney.Server.GameServer.Server.Packet.Send.Archive;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Archive;

[Opcode(CmdIds.GetArchiveDataCsReq)]
public class HandlerGetArchiveDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetArchiveDataScRsp(connection.Player!));
    }
}