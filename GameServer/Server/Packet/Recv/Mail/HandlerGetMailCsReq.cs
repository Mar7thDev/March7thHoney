using March7thHoney.GameServer.Server.Packet.Send.Mail;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Mail;

[Opcode(CmdIds.GetMailCsReq)]
public class HandlerGetMailCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetMailCsReq.Parser.ParseFrom(data);

        // 4.3: GetMailCsReq 分页字段混淆名变更 (start/requestedCount)
        await connection.SendPacket(new PacketGetMailScRsp(connection.Player!, req.NPAFMEBNFOB, req.HFKFNFKIEON));
    }
}
