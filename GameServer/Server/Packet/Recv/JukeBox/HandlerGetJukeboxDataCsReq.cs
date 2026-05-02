using CastoriceHoney.Server.GameServer.Server.Packet.Send.JukeBox;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.JukeBox;

[Opcode(CmdIds.GetJukeboxDataCsReq)]
public class HandlerGetJukeboxDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetJukeboxDataScRsp(connection.Player!));
    }
}