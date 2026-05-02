using CastoriceHoney.Server.GameServer.Server.Packet.Send.Match;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Match;

[Opcode(CmdIds.GetCrossInfoCsReq)]
public class HandlerGetCrossInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetCrossInfoScRsp());
    }
}