using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightBackToPrepareCsReq)]
public class HandlerGridFightBackToPrepareReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        _ = GridFightBackToPrepareReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var inst = player.GridFightManager?.GridFightInstance;

        await connection.SendPacket(new PacketGDMIIBNJJEJ());
        if (inst != null)
        {
            var oldPos = inst.QueuePosition;
            inst.AdvanceQueue();
            inst.PendingAction = new GridFightPendingAction
            {
                QueuePosition = inst.QueuePosition,
                RoundBeginAction = new GridFightRoundBeginActionInfo()
            };
            await connection.SendPacket(new PacketGridFightHandlePendingActionScRsp(inst.QueuePosition));
            await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(player, GridFightSyncKind.PendingAdvance, (oldPos, inst.QueuePosition)));
        }
    }
}
