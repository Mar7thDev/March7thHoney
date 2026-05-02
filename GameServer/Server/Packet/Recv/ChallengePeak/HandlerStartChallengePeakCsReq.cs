using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.ChallengePeak;

[Opcode(CmdIds.StartChallengePeakCsReq)]
public class HandlerStartChallengePeakCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = StartChallengePeakCsReq.Parser.ParseFrom(data);

        await connection.Player!.ChallengePeakManager!.StartChallenge((int)req.PeakLevelId, req.PeakBossBuff,
            req.PeakLevelAvatarIdList.Select(x => (int)x).ToList());
    }
}