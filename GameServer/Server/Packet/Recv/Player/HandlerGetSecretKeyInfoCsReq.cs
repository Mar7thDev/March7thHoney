using CastoriceHoney.Server.GameServer.Server.Packet.Send.Player;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Player;

[Opcode(CmdIds.GetSecretKeyInfoCsReq)]
public class HandlerGetSecretKeyInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetSecretKeyInfoScRsp());
    }
}