using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Database.Inventory;
using March7thHoney.Database.Quests;
using March7thHoney.GameServer.Server.Packet.Send.PlayerSync;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Activity.B51Racing;

public sealed partial class B51RacingManager
{
    private readonly Dictionary<int, QuestInfo> _rewardQuests = [];
    private readonly SemaphoreSlim _rewardLock = new(1, 1);

    private void InitializeRewards()
    {
        var finishTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var questId in Resources.RewardQuestIDs)
        {
            if (!GameData.QuestDataData.TryGetValue(questId, out var quest)) continue;
            if (!GameData.FinishWayData.TryGetValue(quest.FinishWayID, out var finish)) continue;
            _rewardQuests.TryAdd(questId, new QuestInfo
            {
                QuestId = questId,
                QuestStatus = QuestStatus.QuestFinish,
                Progress = finish.Progress,
                FinishTime = finishTime
            });
        }
    }

    public bool ManagesQuest(int questId) => _rewardQuests.ContainsKey(questId);

    public QuestInfo? GetQuestInfo(int questId) => _rewardQuests.GetValueOrDefault(questId);

    public async ValueTask<(Retcode, List<ItemData>?)> TakeQuestReward(int questId)
    {
        await _rewardLock.WaitAsync();
        try
        {
            if (!_rewardQuests.TryGetValue(questId, out var state)) return (Retcode.RetQuestNotAccept, null);
            if (state.QuestStatus == QuestStatus.QuestClose) return (Retcode.RetQuestRewardAlreadyTaken, null);
            if (!GameData.QuestDataData.TryGetValue(questId, out var quest) ||
                !GameData.RewardDataData.TryGetValue(quest.RewardID, out var reward) ||
                reward.GetItems().Any(item => !GameData.ItemConfigData.ContainsKey(item.Item1)) ||
                (reward.Hcoin > 0 && !GameData.ItemConfigData.ContainsKey(1))) return (Retcode.RetFail, null);

            state.QuestStatus = QuestStatus.QuestClose;
            var items = await Player.InventoryManager!.HandleReward(quest.RewardID, sync: false);
            var syncItems = items.Select(item => Player.InventoryManager.Data.MaterialItems
                .Find(material => material.ItemId == item.ItemId)?.Clone() ?? item).ToList();
            await Player.SendPacket(new PacketPlayerSyncScNotify(syncItems));
            await Player.SendPacket(new PacketPlayerSyncScNotify(Player.ToProto()));
            await Player.SendPacket(new PacketPlayerSyncScNotify(state));
            return (Retcode.RetSucc, items);
        }
        finally
        {
            _rewardLock.Release();
        }
    }
}
