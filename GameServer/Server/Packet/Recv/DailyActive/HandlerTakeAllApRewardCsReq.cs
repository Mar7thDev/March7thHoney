using March7thHoney.GameServer.Game.DailyActive;
using March7thHoney.GameServer.Server.Packet.Send.DailyActive;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.DailyActive;

/// <summary>Claims every reachable tier; always succeeds, with an empty list when nothing was claimable.</summary>
[Opcode(CmdIds.TakeAllApRewardCsReq)]
public class HandlerTakeAllApRewardCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        var daily = player.GetComponent<DailyActiveManager>();
        var (levels, items) = await daily.ClaimAll();
        await connection.SendPacket(new PacketTakeAllApRewardScRsp(levels, items));
        if (levels.Count > 0) await connection.SendPacket(new PacketDailyActiveInfoNotify(daily));
    }
}
