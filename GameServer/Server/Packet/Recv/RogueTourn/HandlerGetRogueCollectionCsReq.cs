using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.RogueTourn;

[Opcode(CmdIds.GetRogueCollectionCsReq)]
public class HandlerGetRogueCollectionCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(CmdIds.GetRogueCollectionScRsp);
    }
}