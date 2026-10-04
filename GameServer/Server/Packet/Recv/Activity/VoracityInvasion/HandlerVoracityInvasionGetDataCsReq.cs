using March7thHoney.GameServer.Server.Packet.Send.Activity.VoracityInvasion;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity.VoracityInvasion;

/// <summary>
///     Voracity Invasion module data (10275 -> 10268). The response is named differently from the request, so the
///     generic empty reply never answered it.
/// </summary>
[Opcode(CmdIds.VoracityInvasionGetDataCsReq)]
public class HandlerVoracityInvasionGetDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketGetVoracityInvasionDataScRsp());
    }
}
