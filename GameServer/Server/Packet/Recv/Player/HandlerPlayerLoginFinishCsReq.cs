using March7thHoney.Kcp;
using March7thHoney.Database.Inventory;
using March7thHoney.GameServer.Server.Packet.Send.DailyActive;
using March7thHoney.GameServer.Server.Packet.Send.Player;

namespace March7thHoney.GameServer.Server.Packet.Recv.Player;

[Opcode(CmdIds.PlayerLoginFinishCsReq)]
public class HandlerPlayerLoginFinishCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(CmdIds.PlayerLoginFinishScRsp);
        await connection.SendPacket(new PacketMonthCardRewardNotify([new ItemData
        {
            ItemId = 1,
            Count = 90
        }]));
        await connection.SendPacket(new PacketDailyActiveInfoNotify());
    }
}
