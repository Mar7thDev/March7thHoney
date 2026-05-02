using CastoriceHoney.Server.GameServer.Server.Packet.Send.ChallengePeak;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.ChallengePeak;

[Opcode(CmdIds.SetChallengePeakBossHardModeCsReq)]
public class HandlerSetChallengePeakBossHardModeCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SetChallengePeakBossHardModeCsReq.Parser.ParseFrom(data);

        connection.Player!.ChallengePeakManager!.BossIsHard = req.IsHard;

        await connection.SendPacket(new PacketSetChallengePeakBossHardModeScRsp(req.PeakGroupId, req.IsHard));
    }
}