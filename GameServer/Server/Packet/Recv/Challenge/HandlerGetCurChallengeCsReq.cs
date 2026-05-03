using March7thHoney.GameServer.Server.Packet.Send.Challenge;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Challenge;

[Opcode(CmdIds.GetCurChallengeCsReq)]
public class HandlerGetCurChallengeCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        // Send packet first
        await connection.SendPacket(new PacketGetCurChallengeScRsp(connection.Player!));

        // Update data
        if (connection.Player!.ChallengeManager!.ChallengeInstance != null)
            connection.Player.ChallengeManager.ChallengeInstance.OnUpdate();
    }
}
