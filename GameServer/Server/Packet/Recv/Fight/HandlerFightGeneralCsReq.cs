using CastoriceHoney.Server.GameServer.Server.Packet.Send.Fight;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Fight;

[Opcode(CmdIds.FightGeneralCsReq)]
public class HandlerFightGeneralCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = FightGeneralCsReq.Parser.ParseFrom(data);

        if (connection.MarbleRoom == null || connection.MarblePlayer == null)
        {
            await connection.SendPacket(new PacketFightGeneralScRsp(Retcode.RetFightRoomNotExist));
            return;
        }

        await connection.MarbleRoom.HandleGeneralRequest(connection.MarblePlayer, req.NetworkMsgType,
            req.FightGeneralInfo?.ToByteArray() ?? []);

        await connection.SendPacket(new PacketFightGeneralScRsp(req.NetworkMsgType));
    }
}