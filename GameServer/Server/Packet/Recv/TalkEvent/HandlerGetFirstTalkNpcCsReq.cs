using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.TalkEvent;

[Opcode(CmdIds.GetFirstTalkNpcCsReq)]
public class HandlerGetFirstTalkNpcCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(CmdIds.GetFirstTalkNpcScRsp);
    }
}