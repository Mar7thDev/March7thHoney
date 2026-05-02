using CastoriceHoney.Server.GameServer.Server.Packet.Send.Activity;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Activity;

[Opcode(CmdIds.GetTrialActivityDataCsReq)]
public class HandlerGetTrialActivityDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetTrialActivityDataCsReq.Parser.ParseFrom(data);

        await connection.SendPacket(new PacketGetTrialActivityDataScRsp(connection.Player!));
    }
}