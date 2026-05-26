using March7thHoney.GameServer.Server.Packet.Send.Challenge;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Challenge;

[Opcode(CmdIds.SetChallengeTierceLineupCsReq)]
public class HandlerSetChallengeTierceLineupCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        // Client preconfigures per-node lineups via the editing UI, but the actual roster is also
        // forwarded inside StartChallengeTierceCsReq.stage_info_list. We accept and acknowledge here
        // so the editing UI stays responsive; no state mutation is needed for the gameplay loop.
        await connection.SendPacket(new PacketSetChallengeTierceLineupScRsp());
    }
}
