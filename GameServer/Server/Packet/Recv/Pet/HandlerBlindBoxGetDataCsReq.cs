using March7thHoney.GameServer.Server.Packet.Send.Pet;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Pet;

[Opcode(CmdIds.BlindBoxGetDataCsReq)]
public class HandlerBlindBoxGetDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketBlindBoxGetDataScRsp());
    }
}
