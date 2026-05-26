using March7thHoney.GameServer.Server.Packet.Send.Challenge;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Challenge;

[Opcode(CmdIds.LeaveChallengeTierceCsReq)]
public class HandlerLeaveChallengeTierceCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.Player!.ChallengeTierceManager!.Leave();
        await connection.SendPacket(new PacketLeaveChallengeTierceScRsp());
    }
}
