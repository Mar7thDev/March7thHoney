using March7thHoney.GameServer.Server.Packet.Send.Challenge;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Challenge;

[Opcode(CmdIds.SetChallengeTierceLineupCsReq)]
public class HandlerSetChallengeTierceLineupCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SetChallengeTierceLineupCsReq.Parser.ParseFrom(data);
        connection.Player!.ChallengeTierceManager!.PersistEditorLineups(req.ChallengeId, req.StageInfoList);
        await connection.SendPacket(new PacketSetChallengeTierceLineupScRsp());
    }
}
