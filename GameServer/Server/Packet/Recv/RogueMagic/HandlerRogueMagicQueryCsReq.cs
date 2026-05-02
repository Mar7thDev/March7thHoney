using CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueMagic;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.RogueMagic;

[Opcode(CmdIds.RogueMagicQueryCsReq)]
public class HandlerRogueMagicQueryCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var player = connection.Player!;

        await connection.SendPacket(new PacketRogueMagicQueryScRsp(player));
    }
}