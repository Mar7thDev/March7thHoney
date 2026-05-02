using March7thHoney.GameServer.Server.Packet.Send.Match;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Match;

[Opcode(CmdIds.GetCrossInfoCsReq)]
public class HandlerGetCrossInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetCrossInfoScRsp());
    }
}