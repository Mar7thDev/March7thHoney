using CastoriceHoney.Server.GameServer.Server.Packet.Send.Offering;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Offering;

[Opcode(CmdIds.SubmitOfferingItemCsReq)]
public class HandlerSubmitOfferingItemCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SubmitOfferingItemCsReq.Parser.ParseFrom(data);

        var res = await connection.Player!.OfferingManager!.SubmitOfferingItem((int)req.OfferingId);

        await connection.SendPacket(new PacketSubmitOfferingItemScRsp(res.Item1, res.data));
    }
}