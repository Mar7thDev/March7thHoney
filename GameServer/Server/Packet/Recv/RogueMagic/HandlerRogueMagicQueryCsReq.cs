using March7thHoney.GameServer.Server.Packet.Send.RogueMagic;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.RogueMagic;

[Opcode(CmdIds.RogueMagicQueryCsReq)]
public class HandlerRogueMagicQueryCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var player = connection.Player!;

        await connection.SendPacket(new PacketRogueMagicQueryScRsp(player));
    }
}