using March7thHoney.GameServer.Server.Packet.Send.Rogue;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Rogue;

[Opcode(CmdIds.GetRogueInitialScoreCsReq)]
public class HandlerGetRogueInitialScoreCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetRogueInitialScoreScRsp(connection.Player!));
    }
}