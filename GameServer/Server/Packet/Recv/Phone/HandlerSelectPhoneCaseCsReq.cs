using CastoriceHoney.Server.GameServer.Server.Packet.Send.Phone;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Phone;

[Opcode(CmdIds.SelectPhoneCaseCsReq)]
public class HandlerSelectPhoneCaseCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SelectPhoneCaseCsReq.Parser.ParseFrom(data);

        connection.Player!.Data.PhoneCase = (int)req.PhoneCaseId;

        await connection.SendPacket(new PacketSelectPhoneCaseScRsp(req.PhoneCaseId));
    }
}

