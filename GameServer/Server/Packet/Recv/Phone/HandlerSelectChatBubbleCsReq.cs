using March7thHoney.GameServer.Server.Packet.Send.Phone;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Phone;

[Opcode(CmdIds.SelectChatBubbleCsReq)]
public class HandlerSelectChatBubbleCsReq : Handler<SelectChatBubbleCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, SelectChatBubbleCsReq req)
    {
        player.Data.ChatBubble = (int)req.BubbleId;

        await connection.SendPacket(new PacketSelectChatBubbleScRsp(req.BubbleId));
        // TODO 4.3: TrainCakeCatch 灏忔父鎴忔殏鍋滅紪璇戯紝涓嶅啀鍚戝叾澶у巺骞挎挱鐜╁鐘舵€佸彉鏇?
    }
}
