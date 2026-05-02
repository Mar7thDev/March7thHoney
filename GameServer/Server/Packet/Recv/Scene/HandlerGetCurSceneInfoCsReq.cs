using CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Scene;

[Opcode(CmdIds.GetCurSceneInfoCsReq)]
public class HandlerGetCurSceneInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetCurSceneInfoScRsp(connection.Player!));
    }
}