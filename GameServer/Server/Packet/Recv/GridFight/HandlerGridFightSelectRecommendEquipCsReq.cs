using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightSelectRecommendEquipCsReq)]
public class HandlerGridFightSelectRecommendEquipCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightSelectRecommendEquipCsReq.Parser.ParseFrom(data);
        await connection.SendPacket(new PacketGridFightHandlePendingActionScRsp(req.QueuePosition));
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!));
    }
}
