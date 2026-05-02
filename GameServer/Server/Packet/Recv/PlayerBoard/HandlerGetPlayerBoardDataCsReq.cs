using CastoriceHoney.Server.GameServer.Server.Packet.Send.PlayerBoard;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.PlayerBoard;

[Opcode(CmdIds.GetPlayerBoardDataCsReq)]
public class HandlerGetPlayerBoardDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetPlayerBoardDataScRsp(connection.Player!));
    }
}