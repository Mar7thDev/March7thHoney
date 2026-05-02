using CastoriceHoney.Server.GameServer.Server.Packet.Send.EraFlipper;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.EraFlipper;

[Opcode(CmdIds.GetEraFlipperDataCsReq)]
public class HandlerGetEraFlipperDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetEraFlipperDataScRsp(connection.Player!));
    }
}