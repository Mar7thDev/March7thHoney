using CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueCommon;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.RogueCommon;

[Opcode(CmdIds.GetRogueHandbookDataCsReq)]
public class HandlerGetRogueHandbookDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetRogueHandbookDataScRsp());
    }
}