using March7thHoney.GameServer.Server.Packet.Send.Challenge;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Challenge;

[Opcode(CmdIds.StartChallengeTierceNextStageCsReq)]
public class HandlerStartChallengeTierceNextStageCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var player = connection.Player!;
        var ok = await player.ChallengeTierceManager!.NextStage();
        if (!ok)
        {
            await connection.SendPacket(new PacketStartChallengeTierceNextStageScRsp((uint)Retcode.RetChallengeNotExist));
            return;
        }
        await connection.SendPacket(new PacketStartChallengeTierceNextStageScRsp(player,
            player.ChallengeTierceManager!.CurrentInstance!));
    }
}
