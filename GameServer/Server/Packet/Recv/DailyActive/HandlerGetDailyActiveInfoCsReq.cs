using March7thHoney.GameServer.Game.DailyActive;
using March7thHoney.GameServer.Server.Packet.Send.DailyActive;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.DailyActive;

/// <summary>
///     The panel query. The window is brought to today here too, so a day change mid-session shows the new set.
///     Answered with both the response and the notify: the official capture carries the data in the notify.
/// </summary>
[Opcode(CmdIds.GetDailyActiveInfoCsReq)]
public class HandlerGetDailyActiveInfoCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        var daily = player.GetComponent<DailyActiveManager>();
        await daily.EnsureToday();
        await connection.SendPacket(new PacketGetDailyActiveInfoScRsp(daily));
        await connection.SendPacket(new PacketDailyActiveInfoNotify(daily));
    }
}
