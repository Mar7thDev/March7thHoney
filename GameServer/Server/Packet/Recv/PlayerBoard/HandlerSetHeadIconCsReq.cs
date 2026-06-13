using March7thHoney.GameServer.Server.Packet.Send.PlayerBoard;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.PlayerBoard;

[Opcode(CmdIds.SetHeadIconCsReq)]
public class HandlerSetHeadIconCsReq : Handler<SetHeadIconCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, SetHeadIconCsReq req)
    {
        if (req == null) return;
        player.Data.HeadIcon = (int)req.Id;

        await connection.SendPacket(new PacketSetHeadIconScRsp(player));
        // TODO 4.3: TrainCakeCatch 灏忔父鎴忔殏鍋滅紪璇戯紝涓嶅啀鍚戝叾澶у巺骞挎挱鐜╁鐘舵€佸彉鏇?
    }
}
