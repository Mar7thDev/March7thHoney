using CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueTourn;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.RogueTourn;

[Opcode(CmdIds.RogueTournQueryCsReq)]
public class HandlerRogueTournQueryCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketRogueTournQueryScRsp(connection.Player!));
    }
}