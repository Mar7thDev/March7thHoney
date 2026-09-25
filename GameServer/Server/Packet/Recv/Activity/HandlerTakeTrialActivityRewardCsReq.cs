using March7thHoney.Data;
using March7thHoney.Database;
using March7thHoney.Database.Inventory;
using March7thHoney.GameServer.Game.Activity.Activities;
using March7thHoney.GameServer.Game.Sync;
using March7thHoney.GameServer.Server.Packet.Send.Activity;
using March7thHoney.GameServer.Server.Packet.Send.PlayerSync;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity;

[Opcode(CmdIds.TakeTrialActivityRewardCsReq)]
public class HandlerTakeTrialActivityRewardCsReq : Handler<TakeTrialActivityRewardCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, TakeTrialActivityRewardCsReq req)
    {
        var stage = TrialActivityInstance.GetTrialStage((int)req.StageId);
        if (stage == null)
        {
            await connection.SendPacket(new PacketTakeTrialActivityRewardScRsp(req.StageId, Retcode.RetReqParaInvalid));
            return;
        }

        // Resolve the payout before touching the record so a missing reward row cannot burn the one-time claim.
        var record = player.ActivityManager!.Data.TrialActivityData.Activities
            .Find(x => x.StageId == (int)req.StageId);
        if (!GameData.RewardDataData.TryGetValue(stage.RewardID, out var reward) || record == null ||
            record.TakenReward)
        {
            await connection.SendPacket(new PacketTakeTrialActivityRewardScRsp(req.StageId, Retcode.RetFail));
            return;
        }

        // Mark before granting: a repeated request arriving during the grant must see the reward as taken.
        record.TakenReward = true;
        DatabaseHelper.MarkDirty(player.Uid);

        var granted = new List<ItemData>();
        if (reward.Hcoin > 0)
        {
            player.Data.Hcoin += reward.Hcoin;
            granted.Add(new ItemData { ItemId = 1, Count = reward.Hcoin });
        }

        // AddItem syncs each item's new total itself; the returned copies carry the granted amounts.
        foreach (var (itemId, count) in reward.GetItems())
        {
            var item = await player.InventoryManager!.AddItem(itemId, count, false);
            if (item != null) granted.Add(item);
        }

        // Official claim flow: PlayerSyncScNotify then TakeTrialActivityRewardScRsp, whose reward list drives the
        // client's reward display (no separate ScenePlaneEventScNotify, which would show the items twice).
        await player.SendPacket(new PacketPlayerSyncScNotify(new List<BaseSyncData>
            { new BasicInfoSyncData(player.ToProto()) }));
        await connection.SendPacket(new PacketTakeTrialActivityRewardScRsp(req.StageId, granted));
    }
}
