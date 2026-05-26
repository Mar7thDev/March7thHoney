using March7thHoney.GameServer.Server.Packet.Send.Challenge;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Challenge;

[Opcode(CmdIds.StartChallengeTierceCsReq)]
public class HandlerStartChallengeTierceCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = StartChallengeTierceCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;

        var ok = await player.ChallengeTierceManager!.StartChallenge(req.ChallengeId, req.StageInfoList);
        if (!ok)
        {
            await connection.SendPacket(new PacketStartChallengeTierceScRsp((uint)Retcode.RetChallengeNotExist));
            return;
        }

        await connection.SendPacket(new PacketStartChallengeTierceScRsp(player,
            player.ChallengeTierceManager!.CurrentInstance!));
    }
}
