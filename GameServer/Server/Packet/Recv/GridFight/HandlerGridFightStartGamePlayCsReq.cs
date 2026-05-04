using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightStartGamePlayCsReq)]
public class HandlerGridFightStartGamePlayCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightStartGamePlayCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var (_, inst) = player.GridFightManager!.StartGamePlay(req.Season, req.DivisionId, req.IsOverlock);
        if (inst != null && inst.PendingAction == null)
        {
            inst.PendingAction = new GridFightPendingAction
            {
                QueuePosition = inst.QueuePosition,
                PortalBuffAction = new GridFightPortalBuffActionInfo
                {
                    FCHPJKAIBHB = 1,
                    GridFightPortalBuffList = { 1115u, 106u, 147u }
                }
            };
        }
        await connection.SendPacket(new PacketINHDFEIOBNK(player));
    }
}
