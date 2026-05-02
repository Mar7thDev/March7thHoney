using CastoriceHoney.Server.GameServer.Server.Packet.Send.Friend;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetAssistListCsReq)]
public class HandlerGetAssistListCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetAssistListScRsp());
    }
}