using CastoriceHoney.Server.GameServer.Server.Packet.Send.Player;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Player;

[Opcode(CmdIds.GetVideoVersionKeyCsReq)]
public class HandlerGetVideoVersionKeyCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetVideoVersionKeyScRsp());
    }
}