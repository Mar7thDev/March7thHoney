using CastoriceHoney.Server.GameServer.Server.Packet.Send.StoryLine;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.StoryLine;

[Opcode(CmdIds.GetStoryLineInfoCsReq)]
public class HandlerGetStoryLineInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetStoryLineInfoScRsp(connection.Player!));
    }
}