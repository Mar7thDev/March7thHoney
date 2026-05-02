using March7thHoney.GameServer.Server.Packet.Send.MapRotation;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.MapRotation;

[Opcode(CmdIds.RemoveRotaterCsReq)]
public class HandlerRemoveRotaterCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RemoveRotaterCsReq.Parser.ParseFrom(data);
        await connection.SendPacket(new PacketRemoveRotaterScRsp(connection.Player!, req));
    }
}