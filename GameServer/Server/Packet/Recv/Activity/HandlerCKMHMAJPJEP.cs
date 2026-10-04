using March7thHoney.GameServer.Server.Packet.Send.Activity;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity;

/// <summary>
///     4.7 activity data query (command group 10784-10800, challenge/avatar/battle activity), sent at login once
///     the 4.7 activities are scheduled. Unanswered, the client keeps its loading spinner up; reply with the
///     group's list response (10788) and no entries.
/// </summary>
[Opcode(CmdIds.CKMHMAJPJEP)]
public class HandlerCKMHMAJPJEP : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketPAMDEKPLLEL());
    }
}
