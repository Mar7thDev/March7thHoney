using March7thHoney.GameServer.Server.Packet.Send.Message;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Message;

/// <summary>
///     The staged message performance finishes. The official response echoes the request payload (section id,
///     the chosen bubbles, an empty reward) - that echo is how the client correlates and closes the performance;
///     a bare retcode left the client re-sending the request every ~5 s after the Dan Heng message and the
///     player soft-locked. 4.5 nests the payload (FinishPerformSectionIdCsReq.HBMLBLAJKKL /
///     ScRsp.MIINDKFMBBM); 4.4 kept the same fields flat.
/// </summary>
[Opcode(CmdIds.FinishPerformSectionIdCsReq)]
public class HandlerFinishPerformSectionIdCsReq : Handler<FinishPerformSectionIdCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, FinishPerformSectionIdCsReq req)
    {
        var payload = req.HBMLBLAJKKL;
        var sectionId = (int)(payload?.MGPGDLIJKLM ?? 0);

        // Same section-finish path as FinishSectionIdCsReq; the manager syncs the group/section status and
        // fans out the MessageSectionFinish / MessagePerformSectionFinish mission types. The picked items are
        // recorded first: they are the section's history, and FinishSection only accepts a running section.
        if (sectionId != 0 && player.MessageManager?.GetMessageSectionData(sectionId) != null)
        {
            if (payload != null)
                player.MessageManager.RecordConfirmedItems(sectionId,
                    payload.DPPAFBAHEFH.Select(item => (int)item.ItemId));
            await player.MessageManager.FinishSection(sectionId);
        }

        await connection.SendPacket(new PacketFinishPerformSectionIdScRsp(payload));
    }
}
