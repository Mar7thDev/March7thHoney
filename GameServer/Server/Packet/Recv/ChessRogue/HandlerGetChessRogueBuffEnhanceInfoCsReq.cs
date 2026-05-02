using CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.ChessRogue;

[Opcode(CmdIds.GetChessRogueBuffEnhanceInfoCsReq)]
public class HandlerGetChessRogueBuffEnhanceInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetChessRogueBuffEnhanceInfoScRsp(connection.Player!));
    }
}