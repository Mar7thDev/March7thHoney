using CastoriceHoney.Server.GameServer.Server.Packet.Send.Quest;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Quest;

[Opcode(CmdIds.GetQuestDataCsReq)]
public class HandlerGetQuestDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetQuestDataScRsp(connection.Player!));
    }
}