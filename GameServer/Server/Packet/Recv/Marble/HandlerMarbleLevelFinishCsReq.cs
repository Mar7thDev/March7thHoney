using CastoriceHoney.Server.GameServer.Server.Packet.Send.Marble;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Marble;

[Opcode(CmdIds.MarbleLevelFinishCsReq)]
public class HandlerMarbleLevelFinishCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = MarbleLevelFinishCsReq.Parser.ParseFrom(data);

        await connection.SendPacket(new PacketMarbleLevelFinishScRsp(req.MarbleLevelId));
    }
}