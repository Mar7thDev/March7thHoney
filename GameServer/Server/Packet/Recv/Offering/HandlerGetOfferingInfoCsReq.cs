using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.Offering;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Offering;

[Opcode(CmdIds.GetOfferingInfoCsReq)]
public class HandlerGetOfferingInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetOfferingInfoCsReq.Parser.ParseFrom(data);

        List<OfferingTypeData> dataList = [];
        dataList.AddRange(req.OfferingIdList.Select(id => connection.Player!.OfferingManager!.GetOfferingData((int)id))
            .OfType<OfferingTypeData>());

        await connection.SendPacket(new PacketGetOfferingInfoScRsp(dataList));
    }
}