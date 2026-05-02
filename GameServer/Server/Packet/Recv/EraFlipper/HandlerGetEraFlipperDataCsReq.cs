using March7thHoney.GameServer.Server.Packet.Send.EraFlipper;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.EraFlipper;

[Opcode(CmdIds.GetEraFlipperDataCsReq)]
public class HandlerGetEraFlipperDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetEraFlipperDataScRsp(connection.Player!));
    }
}