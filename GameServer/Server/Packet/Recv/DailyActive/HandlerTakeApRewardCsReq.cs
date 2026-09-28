using March7thHoney.GameServer.Game.DailyActive;
using March7thHoney.GameServer.Server.Packet.Send.DailyActive;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.DailyActive;

[Opcode(CmdIds.TakeApRewardCsReq)]
public class HandlerTakeApRewardCsReq : Handler<TakeApRewardCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, TakeApRewardCsReq req)
    {
        var daily = player.GetComponent<DailyActiveManager>();
        var (retcode, items) = await daily.ClaimTier((int)req.Level);
        await connection.SendPacket(new PacketTakeApRewardScRsp(retcode, req.Level, items));
        if (retcode == Retcode.RetSucc) await connection.SendPacket(new PacketDailyActiveInfoNotify(daily));
    }
}
