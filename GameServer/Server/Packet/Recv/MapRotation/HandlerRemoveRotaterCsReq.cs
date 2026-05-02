using CastoriceHoney.Server.GameServer.Server.Packet.Send.MapRotation;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.MapRotation;

[Opcode(CmdIds.RemoveRotaterCsReq)]
public class HandlerRemoveRotaterCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RemoveRotaterCsReq.Parser.ParseFrom(data);
        await connection.SendPacket(new PacketRemoveRotaterScRsp(connection.Player!, req));
    }
}