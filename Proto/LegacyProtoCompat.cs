using Google.Protobuf;
using Google.Protobuf.Collections;

namespace March7thHoney.Proto;

// Temporary compatibility models for symbols removed/renamed in newer proto sets.
public class ChessRogueDice
{
    public uint BranchId { get; set; }
    public RepeatedField<ChessRogueDiceSurfaceInfo> SurfaceList { get; } = [];
}

public class ChessRogueDiceSurfaceInfo
{
    public uint DiceSlotId { get; set; }
    public uint DiceSurfaceId { get; set; }
}

public class FriendDevelopmentInfo
{
    public long Time { get; set; }
    public DevelopmentType DevelopmentType { get; set; }
    public FriendRogueDevelopmentInfo? RogueDevelopmentInfo { get; set; }
    public FriendChallengeDevelopmentInfo? ChallengeDevelopmentInfo { get; set; }
    public uint AvatarId { get; set; }
    public uint EquipmentTid { get; set; }
    public FriendRogueTournDevelopmentInfo? RogueTournDevelopmentInfo { get; set; }
    public FriendChallengePeakDevelopmentInfo? ChallengePeakDevelopmentInfo { get; set; }
}

public class FriendRogueDevelopmentInfo
{
    public uint AreaId { get; set; }
}

public class FriendChallengeDevelopmentInfo
{
    public uint ChallengeId { get; set; }
}

public class FriendRogueTournDevelopmentInfo
{
    public uint AreaId { get; set; }
    public uint FinishTournDifficulty { get; set; }
}

public class FriendChallengePeakDevelopmentInfo
{
    public uint PeakLevelId { get; set; }
}

public class ChallengeGroupStatistics
{
    public uint GroupId { get; set; }
    public uint GroupTotalStars { get; set; }
    public MemoryGroupStatistics? MemoryGroup { get; set; }
    public StoryGroupStatistics? StoryGroup { get; set; }
    public BossGroupStatistics? BossGroup { get; set; }
}

public class MemoryGroupStatistics
{
    public uint RecordId { get; set; }
    public MemoryStatisticsInfo SttInfo { get; set; } = new();
}

public class StoryGroupStatistics
{
    public uint RecordId { get; set; }
    public StoryStatisticsInfo SttInfo { get; set; } = new();
}

public class BossGroupStatistics
{
    public uint RecordId { get; set; }
    public BossStatisticsInfo SttInfo { get; set; } = new();
}

public class MemoryStatisticsInfo
{
    public uint CurLevelStars { get; set; }
    public uint Level { get; set; }
    public uint RoundCount { get; set; }
    public RepeatedField<ChallengeLineupList> LineupList { get; } = [];
}

public class StoryStatisticsInfo
{
    public uint CurLevelStars { get; set; }
    public uint Level { get; set; }
    public RepeatedField<ChallengeLineupList> LineupList { get; } = [];
    public uint BuffOne { get; set; }
    public uint BuffTwo { get; set; }
    public uint ScoreId { get; set; }
}

public class BossStatisticsInfo
{
    public uint CurLevelStars { get; set; }
    public uint Level { get; set; }
    public RepeatedField<ChallengeLineupList> LineupList { get; } = [];
    public uint BuffOne { get; set; }
    public uint BuffTwo { get; set; }
    public uint ScoreId { get; set; }
}

public class ChallengePeakAvatar
{
    public uint AvatarId { get; set; }
    public uint EquipmentUniqueId { get; set; }
    public RepeatedField<EquipRelic> RelicList { get; } = [];
}

public class LobbyPlayerBasicInfo
{
    public string? Nickname { get; set; }
    public uint Level { get; set; }
    public uint LobbyHeadIconId { get; set; }
    public PlatformType Platform { get; set; }
    public uint Uid { get; set; }
}

public class HandInfo
{
    public uint ConfigId { get; set; }
    public ByteString HandByteValue { get; set; } = ByteString.Empty;
    public uint HandCoinNum { get; set; }
    public MotionInfo HandMotion { get; set; } = new();
    public uint HandState { get; set; }
}

public class PrivacySettings
{
    public bool DisplayChallengeLineup { get; set; }
    public bool DisplayActiveState { get; set; }
    public bool DisplayRecentlyState { get; set; }
    public bool DisplayBattleRecord { get; set; }
    public bool DisplayCollection { get; set; }
}

public class RogueCommonBuff
{
    public uint BuffId { get; set; }
    public uint BuffLevel { get; set; }
}

public class FormulaInfo
{
    public uint FormulaId { get; set; }
    public bool IsExpand { get; set; }
    public RepeatedField<FormulaBuffTypeInfo> FormulaBuffTypeList { get; } = [];
}

public class FormulaBuffTypeInfo
{
    public uint Key { get; set; }
    public int FormulaBuffNum { get; set; }
}

public enum RogueCommonActionResultSourceType
{
    None = 0,
    Select = 1,
    Dialogue = 2,
    Buff = 3,
    Reforge = 4,
    Enhance = 5,
    MagicUnitCompose = 6
}

public enum RogueCommonActionResultDisplayType
{
    None = 0,
    Single = 1,
    Multi = 2
}

public class RogueCommonActionResult
{
    public RogueCommonActionResultData RogueAction { get; set; } = new();
    public RogueCommonActionResultSourceType Source { get; set; }
}

public class RogueCommonActionResultData
{
    public RogueTitanBlessEvent? TitanBlessEvent { get; set; }
    public RogueCommonFormula? GetFormulaList { get; set; }
    public RogueCommonRemoveFormula? RemoveFormulaList { get; set; }
    public RogueCommonExpandedFormula? ExpandFormulaList { get; set; }
    public RogueCommonContractFormula? ContractFormulaList { get; set; }
    public RogueCommonBuff? GetBuffList { get; set; }
    public RogueCommonBuff? RemoveBuffList { get; set; }
}

public class RogueTitanBlessEvent
{
    public uint EventUniqueId { get; set; }
}

public class RogueCommonFormula
{
    public FormulaInfo? FormulaInfo { get; set; }
}

public class RogueCommonRemoveFormula
{
    public FormulaInfo? FormulaInfo { get; set; }
}

public class RogueCommonExpandedFormula
{
    public FormulaInfo? FormulaInfo { get; set; }
}

public class RogueCommonContractFormula
{
    public FormulaInfo? FormulaInfo { get; set; }
}

public class PlayerExtraSettingsInfo
{
}

public class PlayerCollectionInfo
{
}

public sealed partial class Avatar
{
    public uint Rank { get; set; }
    public uint DressedSkinId { get; set; }
    public uint CurEnhanceId { get; set; }
    public RepeatedField<EquipRelic> EquipRelicList { get; } = [];
    public RepeatedField<AvatarSkillTree> SkilltreeList { get; } = [];
}

public sealed partial class BattleAvatar
{
    public uint AvatarEnhanceId { get; set; }
}

public sealed partial class GachaInfo
{
    public string? DetailWebview { get; set; }
    public string? DropHistoryWebview { get; set; }
}

public sealed partial class HeadFrameInfo
{
    public uint HeadFrameId { get; set; }
}

public sealed partial class PlayerSettingInfo
{
    public bool DisplayChallengeLineup { get; set; }
    public bool DisplayActiveState { get; set; }
    public bool DisplayRecentlyState { get; set; }
    public bool DisplayBattleRecord { get; set; }
    public bool DisplayCollection { get; set; }
    public PlayerExtraSettingsInfo? ExtraSettingsInfo { get; set; }
}

public sealed partial class PlayerSimpleInfo
{
    public long LogoutTime { get; set; }
    public uint ChatBubble { get; set; }
    public HeadFrameInfo? HeadIconFrameInfo { get; set; }
    public RepeatedField<AssistSimpleInfo> AssistInfoList { get; } = [];
}

public sealed partial class PlayerDetailInfo
{
    public uint PersonalCard { get; set; }
    public bool EMOBIJBDKEI { get; set; }
    public PrivacySettings? PrivacySettings { get; set; }
    public HeadFrameInfo? HeadFrame { get; set; }
}

public sealed partial class PlayerRecordInfo
{
    public uint CollectAvatarCount { get; set; }
    public uint CollectEquipmentCount { get; set; }
    public uint CollectRelicCount { get; set; }
    public uint CollectAchievementCount { get; set; }
    public PlayerCollectionInfo? CollectionInfo { get; set; }
    public uint CollectDiscCount { get; set; }
}
