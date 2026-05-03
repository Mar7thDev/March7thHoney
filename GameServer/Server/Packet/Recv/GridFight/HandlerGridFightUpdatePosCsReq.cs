using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightUpdatePosCsReq)]
public class HandlerGridFightUpdatePosCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightUpdatePosCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var gridFight = player.GridFightManager?.GridFightInstance;
        if (gridFight == null)
        {
            await connection.SendPacket(new PacketGridFightUpdatePosScRsp());
            return;
        }

        var posList = req.GridFightPosInfoList.ToList();
        gridFight.ApplyPositionList(posList);

        await connection.SendPacket(new PacketGridFightUpdatePosScRsp(posList));
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(player, posList));
    }
}
