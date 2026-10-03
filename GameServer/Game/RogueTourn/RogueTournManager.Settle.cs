using March7thHoney.Data;
using March7thHoney.Database.Inventory;
using March7thHoney.Database.RogueTourn;
using March7thHoney.Enums.TournRogue;
using MemoryPack;

namespace March7thHoney.GameServer.Game.RogueTourn;

public partial class RogueTournManager
{
    // RogueTournConstCommon: RogueTourn_Archive_MaxCount / RogueTourn_Archive_NameMaxLength.
    private const int ArchiveMaxCount = 4;
    private const int ArchiveNameMaxLength = 24;

    /// <summary>
    ///     Flips a run whose cleared room is the last room of the last layer to Finished. The official does this from
    ///     the last battle's result and pushes the run three times, which is what opens the client's settle page.
    /// </summary>
    public bool FinishRun()
    {
        var run = RunInstance;
        if (run == null || run.Status != RogueTournStates.RunDoing) return false;
        if (run.CurRoom() is not { Status: RogueTournStates.RoomFinish } || !run.AtFinalRoom()) return false;

        run.Status = RogueTournStates.RunFinished;
        run.LevelStatus = RogueTournStates.LevelAllRoomsDone;
        Save();
        return true;
    }

    public async ValueTask<bool> FinishRunIfLastRoom()
    {
        if (RunInstance?.AtFinalRoom() != true || !FinishRun()) return false;
        await SendLevelNotify();
        await SendLevelNotify(false);
        await SendLevelNotify(false);
        return true;
    }

    /// <summary>
    ///     The run's settle score from the season's authored rule (one value per cleared room of a counted type) and the
    ///     season exp it is worth. No table authors either number; without a rule the run scores nothing.
    /// </summary>
    public (int Score, int Gain) RunScore()
    {
        var run = RunInstance;
        var plan = run == null ? null : RogueTournResources.SeasonPlan((RogueTournModeEnum)run.TournMode)?.ScorePlan();
        if (run == null || plan == null) return (0, 0);

        var score = run.Layers.SelectMany(layer => layer.Rooms)
            .Where(room => room.Status == RogueTournStates.RoomFinish && plan.RoomTypes.Contains(room.RoomType))
            .Sum(_ => plan.PerRoom);
        return (score, score * plan.ExpPerScore);
    }

    // The settle page already shows the total the confirmation is about to grant.
    public (int Score, int Gain, int Total) SettleScore()
    {
        var (score, gain) = RunScore();
        return (score, gain, Data.Exp + gain);
    }

    /// <summary>
    ///     Settles a Finished run into the archive slot the client chose (0 = the next free one), replacing that slot's
    ///     entry and keeping at most the shipped archive limit, and grants the run's season exp. Returns the slot, or 0
    ///     when the run cannot settle.
    /// </summary>
    public int SettleRun(string? name, int slot)
    {
        var run = RunInstance;
        if (run == null || run.Status != RogueTournStates.RunFinished || run.LevelStatus == RogueTournStates.LevelSettling)
            return 0;

        name ??= "";
        if (name.Length > ArchiveNameMaxLength) name = name[..ArchiveNameMaxLength];
        if (slot <= 0) slot = Data.Archives.Count + 1;

        var (_, gain) = RunScore();
        run.LevelStatus = RogueTournStates.LevelSettling;
        run.LevelReason = RogueTournStates.ReasonOnSettle;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var entry = new RogueTournArchiveData
        {
            Slot = slot,
            Name = name,
            Avatars = [.. run.BaseAvatarIds],
            Data = Convert.ToBase64String(MemoryPackSerializer.Serialize(run)),
            Time = now,
            EndTime = now
        };
        var index = Data.Archives.FindIndex(archive => archive.Slot == slot);
        if (index >= 0) Data.Archives[index] = entry;
        else Data.Archives.Add(entry);
        if (Data.Archives.Count > ArchiveMaxCount) Data.Archives.RemoveRange(0, Data.Archives.Count - ArchiveMaxCount);

        Data.Exp += gain;
        if (!Data.AreaProgress.TryGetValue(run.AreaId, out var progress))
            Data.AreaProgress[run.AreaId] = progress = new RogueTournAreaProgressData { IsUnlocked = true };
        progress.IsFirstPassed = true;

        Save();
        return slot;
    }

    public RogueTournArchiveData? ArchiveOf(int slot)
    {
        return Data.Archives.FirstOrDefault(archive => archive.Slot == slot);
    }

    /// <summary>
    ///     Claims the exp-ladder levels asked for. Each level is validated alone (already taken → exp not reached →
    ///     row missing) and skipped on failure, so one bad id cannot block the rest; the reward items are granted.
    /// </summary>
    public async ValueTask<(List<int> Levels, List<ItemData> Items)> TakeExpRewards(int mainTournId, IEnumerable<int> levels)
    {
        List<int> paid = [];
        List<ItemData> items = [];
        if (mainTournId == 0) return (paid, items);

        foreach (var level in levels.Distinct())
        {
            if (level == 0 || Data.TakenLevelRewards.Contains(level)) continue;
            if (!GameData.RogueTournExpRewardData.TryGetValue(mainTournId << 16 | level, out var row) ||
                row.RewardID == 0 || row.Exp > Data.Exp) continue;
            Data.TakenLevelRewards.Add(level);
            paid.Add(level);
            items.AddRange(await Player.InventoryManager!.HandleReward(row.RewardID, true));
        }

        if (paid.Count > 0) Save();
        return (paid, items);
    }

    // Meeting a Workbench of Creation discovers it in the handbook (DMFAGJOOGJM).
    public void DiscoverWorkbench(int workbenchId)
    {
        if (RogueTournHandbookData.Discover(Handbook.Workbenches, workbenchId)) Save();
    }
}
