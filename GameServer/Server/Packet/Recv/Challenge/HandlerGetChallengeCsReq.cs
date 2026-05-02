using CastoriceHoney.Server.GameServer.Server.Packet.Send.Challenge;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Challenge;

[Opcode(CmdIds.GetChallengeCsReq)]
public class HandlerGetChallengeCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetChallengeScRsp(connection.Player!));
    }
}