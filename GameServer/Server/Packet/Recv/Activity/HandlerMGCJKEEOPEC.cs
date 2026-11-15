using March7thHoney.GameServer.Server.Packet.Send.Activity;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity;

/// <summary>
///     4.7 activity data query (command group 10204-10220, level/record activity), sent at login once the 4.7
///     activities are scheduled. Unanswered, the client keeps its loading spinner up; reply with the group's data
///     response (10208) carrying no progress.
/// </summary>
[Opcode(CmdIds.MGCJKEEOPEC)]
public class HandlerMGCJKEEOPEC : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketLPLCFFGANFC());
    }
}
