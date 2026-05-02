using CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Rogue;

[Opcode(CmdIds.GetRogueInitialScoreCsReq)]
public class HandlerGetRogueInitialScoreCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetRogueInitialScoreScRsp(connection.Player!));
    }
}