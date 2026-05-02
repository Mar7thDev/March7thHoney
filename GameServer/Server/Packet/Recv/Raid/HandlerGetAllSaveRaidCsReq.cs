using CastoriceHoney.Server.GameServer.Server.Packet.Send.Raid;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Raid;

[Opcode(CmdIds.GetAllSaveRaidCsReq)]
public class HandlerGetAllSaveRaidCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetAllSaveRaidScRsp(connection.Player!));
    }
}