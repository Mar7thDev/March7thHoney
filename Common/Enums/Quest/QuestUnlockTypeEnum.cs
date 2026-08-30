namespace March7thHoney.Enums.Quest;

public enum QuestUnlockTypeEnum
{
    Unknown = 0,
    AutoUnlock = 1,
    FinishQuest = 2,
    FinishMission = 3,
    BattlePassWeekly = 4,
    ManualUnlock = 5,

    // 4.5 ExcelOutput/QuestData.json ships this for the CakeCatch mini-game dailies. It is a
    // client-side gate we do not simulate, so it stays a no-op unlock - but it must exist as a
    // named member: the whole QuestData.json load used to abort on it, which cleared
    // ResourceCache.IsComplete and silently suppressed Resource.bin generation.
    CakeCatchNotFinish = 6
}