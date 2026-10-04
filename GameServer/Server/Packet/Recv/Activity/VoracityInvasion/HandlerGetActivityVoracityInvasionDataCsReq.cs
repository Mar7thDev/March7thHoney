using March7thHoney.GameServer.Server.Packet.Send.Activity.VoracityInvasion;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity.VoracityInvasion;

/// <summary>
///     Voracity Invasion world/personal progress query (10675, obfuscated GMHGNOPOONF -> 10668). The activity panel
///     builds its VoracityInvasionWorldActivity from this reply; unanswered, selecting the activity tab loads a nil
///     prefab and the client throws ArgumentNullException in UIPrefabLoader.
/// </summary>
[Opcode(CmdIds.GMHGNOPOONF)]
public class HandlerGetActivityVoracityInvasionDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketGetActivityVoracityInvasionDataScRsp());
    }
}
