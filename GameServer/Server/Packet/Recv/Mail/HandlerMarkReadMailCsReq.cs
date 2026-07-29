using March7thHoney.GameServer.Server.Packet.Send.Mail;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Mail;

[Opcode(CmdIds.MarkReadMailCsReq)]
public class HandlerMarkReadMailCsReq : Handler<PEEIPMKDOOL>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, PEEIPMKDOOL req)
    {
        var mail = player.MailManager!.GetMail((int)req.AKFFJEHMCCB);

        if (mail != null)
        {
            mail.IsRead = true;
            await connection.SendPacket(new PacketMarkReadMailScRsp(req.AKFFJEHMCCB));
        }
        else
        {
            await connection.SendPacket(new PacketMarkReadMailScRsp(Retcode.RetMailMailIdInvalid));
        }
    }
}
