using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Activity.Activities.DiceCombat;

public class DiceCombatAvatarState
{
    public uint Level { get; set; } = DiceCombatConfig.DefaultAvatarLevel;
    public List<uint> DiceIds { get; set; } = new(DiceCombatConfig.DefaultDiceIds);
    public long UnlockTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

public class DiceCombatStageState
{
    public bool Completed { get; set; }
    public long CompletedTime { get; set; }
}

public class DiceCombatActivityInstance : BaseActivityInstance
{
    public Dictionary<uint, DiceCombatAvatarState> Avatars { get; } = new();
    public Dictionary<uint, DiceCombatStageState> Stages { get; } = new();
    public HashSet<uint> FeatureUnlocks { get; } = new();
    public HashSet<uint> ScoreTakenIds { get; } = new();

    public DiceCombatActivityInstance(ActivityManager manager) : base(manager)
    {
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        for (var id = DiceCombatConfig.AvatarIdBegin; id <= DiceCombatConfig.AvatarIdEnd; id++)
            Avatars[id] = new DiceCombatAvatarState { UnlockTime = now };

        foreach (var stageId in DiceCombatConfig.CompletedStageIds)
            Stages[stageId] = new DiceCombatStageState { Completed = true, CompletedTime = now };

        foreach (var id in DiceCombatConfig.FeatureUnlockIds) FeatureUnlocks.Add(id);
        foreach (var id in DiceCombatConfig.ScoreTakenIds) ScoreTakenIds.Add(id);
    }

    public DiceCombatAvatar BuildAvatarProto(uint avatarId, DiceCombatAvatarState state)
    {
        var proto = new DiceCombatAvatar
        {
            DiceAvatarId = avatarId,
            Level = state.Level,
            UnlockTime = state.UnlockTime
        };
        proto.DiceIdList.AddRange(state.DiceIds);
        return proto;
    }

    public void RecordStageFinish(uint stageId, bool isWin)
    {
        if (!isWin) return;
        if (!Stages.TryGetValue(stageId, out var state))
        {
            state = new DiceCombatStageState();
            Stages[stageId] = state;
        }
        state.Completed = true;
        state.CompletedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public DiceCombatAvatarState GetOrCreateAvatar(uint avatarId)
    {
        if (!Avatars.TryGetValue(avatarId, out var state))
        {
            state = new DiceCombatAvatarState();
            Avatars[avatarId] = state;
        }
        return state;
    }
}
