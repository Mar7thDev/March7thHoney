using CastoriceHoney.Server.GameServer.Server.Packet.Send.Mission;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Mission;

[Opcode(CmdIds.GetMainMissionCustomValueCsReq)]
public class HandlerGetMainMissionCustomValueCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetMainMissionCustomValueCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        await connection.SendPacket(new PacketGetMainMissionCustomValueScRsp(req, player));
    }
}