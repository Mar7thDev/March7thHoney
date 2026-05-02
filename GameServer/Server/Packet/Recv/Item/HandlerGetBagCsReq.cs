using CastoriceHoney.Server.GameServer.Server.Packet.Send.Item;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Item;

[Opcode(CmdIds.GetBagCsReq)]
public class HandlerGetBagCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetBagScRsp(connection.Player!));
    }
}