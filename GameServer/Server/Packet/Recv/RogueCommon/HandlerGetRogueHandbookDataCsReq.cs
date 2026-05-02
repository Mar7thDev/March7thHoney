using March7thHoney.GameServer.Server.Packet.Send.RogueCommon;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.RogueCommon;

[Opcode(CmdIds.GetRogueHandbookDataCsReq)]
public class HandlerGetRogueHandbookDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetRogueHandbookDataScRsp());
    }
}