using March7thHoney.GameServer.Game.Activity.Activities;
using March7thHoney.GameServer.Game.Sync;
using March7thHoney.GameServer.Server.Packet.Send.Activity;
using March7thHoney.GameServer.Server.Packet.Send.PlayerSync;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity;

[Opcode(CmdIds.TakeMaterialSubmitActivityRewardCsReq)]
public class HandlerTakeMaterialSubmitActivityRewardCsReq : Handler<TakeMaterialSubmitActivityRewardCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player,
        TakeMaterialSubmitActivityRewardCsReq req)
    {
        var (retcode, items) = await new MaterialSubmitActivity(player).TakeReward((int)req.LNOCOOCMNCI);
        if (retcode == Retcode.RetSucc)
            await player.SendPacket(new PacketPlayerSyncScNotify(new List<BaseSyncData>
                { new BasicInfoSyncData(player.ToProto()) }));
        await connection.SendPacket(
            new PacketTakeMaterialSubmitActivityRewardScRsp(req.LNOCOOCMNCI, retcode, items));
    }
}
