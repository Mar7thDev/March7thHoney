using MemoryPack;
using March7thHoney.Data;
using March7thHoney.Database.Avatar;
using March7thHoney.Database.Inventory;
using March7thHoney.Database.Quests;
using March7thHoney.Proto;
using March7thHoney.Util;
using LineupInfo = March7thHoney.Database.Lineup.LineupInfo;

namespace March7thHoney.Database.Player;

[DbTable("Player")]
public class PlayerData : BaseDatabaseDataHelper
{
    public string? Name { get; set; } = "";
    public string? Signature { get; set; } = "";
    public int Birthday { get; set; } = 0;
    public int CurBasicType { get; set; } = 8002;
    public int HeadIcon { get; set; } = 208002;
    public int PhoneTheme { get; set; } = 221000;
    public int ChatBubble { get; set; } = 220000;
    public int PersonalCard { get; set; } = 253000;
    public int PhoneCase { get; set; } = 254000;
    public int CurrentBgm { get; set; } = 210007;
    public int CurrentPamSkin { get; set; } = 252000;
    public bool IsGenderSet { get; set; } = false;
    public Gender CurrentGender { get; set; } = Gender.Woman;
    public int Level { get; set; } = 1;
    public int Exp { get; set; } = 0;
    public int WorldLevel { get; set; } = 0;
    public int Scoin { get; set; } = 0; // Credits
    public int Hcoin { get; set; } = 0; // Jade
    public int Mcoin { get; set; } = 0; // Crystals
    public int TalentPoints { get; set; } = 0; // Rogue talent points
    public long MonthCardExpireTime { get; set; } = 0;
    public int LastMonthCardRewardDate { get; set; } = 0;
    public bool WelcomeAnnouncePending { get; set; } = false;

    public int Pet { get; set; } = 0;
    public int CurMusicLevel { get; set; }

    public int Stamina { get; set; } = 300;
    public double StaminaReserve { get; set; } = 0;
    public long NextStaminaRecover { get; set; } = 0;

    public Position? Pos { get; set; }

    public Position? Rot { get; set; }

    public PlayerHeadFrameInfo HeadFrame { get; set; } = new();
    public List<int> PlayerOutfitList { get; set; } = [];

    public int PlaneId { get; set; }

    public int FloorId { get; set; }

    public int EntryId { get; set; }

    public long LastActiveTime { get; set; }

    public List<int> TakenLevelReward { get; set; } = [];
    public PrivacySettingsPb PrivacySettings { get; set; } = new();

    public int FakeTimeDate { get; set; } = 0; // yyyyMMdd, 0 = off

    // Language-file code pinned with /language; null follows the client-reported language.
    public string? LanguagePin { get; set; }

    public static PlayerData? GetPlayerByUid(long uid)
    {
        var result = DatabaseHelper.Instance?.GetInstance<PlayerData>((int)uid);
        return result;
    }

    public PlayerBasicInfo ToProto()
    {
        return new PlayerBasicInfo
        {
            Nickname = Name,
            Level = (uint)Level,
            Exp = (uint)Exp,
            WorldLevel = (uint)WorldLevel,
            Scoin = (uint)Scoin,
            Hcoin = (uint)Hcoin,
            Mcoin = (uint)Mcoin,
            Stamina = (uint)Stamina
        };
    }

    // 4.4 lobby member basic info (MOHAGOFLGAP = pre-4.2 LobbyPlayerBasicInfo)
    public MOHAGOFLGAP ToLobbyProto()
    {
        return new MOHAGOFLGAP
        {
            Nickname = Name,
            Level = (uint)Level,
            Icon = (uint)HeadIcon,
            Platform = PlatformType.Pc,
            Uid = (uint)Uid
        };
    }

    public PlayerSimpleInfo ToSimpleProto(FriendOnlineStatus status)
    {
        if (!GameData.ChatBubbleConfigData.ContainsKey(ChatBubble)) // to avoid npe
            ChatBubble = 220000;

        var info = new PlayerSimpleInfo
        {
            Nickname = Name,
            Level = (uint)Level,
            Signature = Signature,
            Uid = (uint)Uid,
            OnlineStatus = status,
            HeadIcon = (uint)HeadIcon,
            Platform = PlatformType.Pc,
            LastActiveTime = LastActiveTime,
            ChatBubbleId = (uint)ChatBubble,
            PersonalCard = (uint)PersonalCard,
            HeadFrameInfo = ToPublicHeadFrameProto(),
            Gender = (uint)CurrentGender
        };

        if (PlayerOutfitList.Count > 0)
            info.PlayerOutfitData = ToPlayerOutfitProto();

        var pos = 0;
        var instance = DatabaseHelper.Instance!.GetInstance<AvatarData>(Uid);
        if (instance == null)
        {
            // Handle the synthetic contacts (command bot / Global Chat): always online.
            var systemAssists = ConfigManager.Config.ServerOption.GetSystemProfileAssistInfo(Uid);
            if (systemAssists != null)
            {
                info.OnlineStatus = FriendOnlineStatus.Online;
                info.AssistSimpleInfoList.AddRange(
                    systemAssists.Select((x, index) =>
                        new AssistSimpleInfo
                        {
                            AvatarId = (uint)x.AvatarId,
                            Level = (uint)x.Level,
                            DressedSkinId = (uint)x.SkinId,
                            Pos = (uint)index
                        }));
            }

            return info;
        }

        foreach (var avatar in instance.AssistAvatars.Select(instance.FindByAnyId))
            if (avatar != null)
                info.AssistSimpleInfoList.Add(new AssistSimpleInfo
                {
                    AvatarId = (uint)avatar.AvatarId,
                    Level = (uint)avatar.Level,
                    DressedSkinId = (uint)avatar.GetCurPathInfo().Skin,
                    Pos = (uint)pos++
                });

        return info;
    }

    public PlayerDetailInfo ToDetailProto()
    {
        var info = new PlayerDetailInfo
        {
            Nickname = Name,
            Level = (uint)Level,
            Signature = Signature,
            IsBanned = false,
            HeadIcon = (uint)HeadIcon,
            Platform = PlatformType.Pc,
            Uid = (uint)Uid,
            WorldLevel = (uint)WorldLevel,
            RecordInfo = new PlayerRecordInfo(),
            HeadFrameInfo = ToPublicHeadFrameProto()
        };

        var avatarInfo = DatabaseHelper.Instance!.GetInstance<AvatarData>(Uid);
        var inventoryInfo = DatabaseHelper.Instance.GetInstance<InventoryData>(Uid);
        var questInfo = DatabaseHelper.Instance.GetInstance<QuestData>(Uid);

        if (avatarInfo == null || inventoryInfo == null || questInfo == null)
        {
            // Handle the synthetic contacts (command bot / Global Chat)
            var systemAssists = ConfigManager.Config.ServerOption.GetSystemProfileAssistInfo(Uid);
            if (systemAssists != null)
                info.AssistAvatarList.AddRange(
                    systemAssists.Select((x, index) =>
                        new DisplayAvatarDetailInfo
                        {
                            AvatarId = (uint)x.AvatarId,
                            Level = (uint)x.Level,
                            DressedSkinId = (uint)x.SkinId,
                            Pos = (uint)index
                        }));
            return info;
        }

        info.RecordInfo = new PlayerRecordInfo
        {
            DIOIIFAHAKK = (uint)avatarInfo.FormalAvatars.Count,
            NECKJBHEKHE = (uint)inventoryInfo.EquipmentItems.Select(x => x.ItemId).ToHashSet().Count,
            KCBINJNIBNO = (uint)inventoryInfo.RelicItems.Count,
            ALGEOLGNFBL = (uint)GameData.AchievementDataData.Values.Select(x => x.QuestID).ToHashSet()
                .Count(x => questInfo.Quests.GetValueOrDefault(x)?.QuestStatus is QuestStatus.QuestFinish
                    or QuestStatus.QuestClose), // count finished achievements
            MHOIJFEBAPA = (uint)GameData.BackGroundMusicData.Count
        };

        var pos = 0;
        foreach (var avatar in avatarInfo.AssistAvatars.Select(avatarInfo.FindByAnyId))
            if (avatar != null)
                info.AssistAvatarList.Add(avatar.ToDetailProto(pos++,
                    new PlayerDataCollection(this, inventoryInfo, new LineupInfo())));

        pos = 0;
        foreach (var avatar in avatarInfo.DisplayAvatars.Select(display =>
                     avatarInfo.FormalAvatars.Find(x => x.BaseAvatarId == display)))
            if (avatar != null)
                info.DisplayAvatarList.Add(avatar.ToDetailProto(pos++,
                    new PlayerDataCollection(this, inventoryInfo, new LineupInfo())));

        return info;
    }

    /// <summary>
    ///     其他玩家看到的头像框: 隐藏时下发空头像框。客户端 PlayerBoardInfo.CreateByPlayerDetailInfo 只按
    ///     HeadFrameInfo.IsValid(物品 ID 非 0 且未过期)决定是否显示; 自己的资料卡始终收到真实头像框, 由本地设置决定显示。
    /// </summary>
    public HeadFrameInfo ToPublicHeadFrameProto() =>
        PrivacySettings.HideHeadFrame ? new HeadFrameInfo() : HeadFrame.ToProto();

    public CJLCPMDGIBO ToPlayerOutfitProto() =>
        new() { EAKOLIJOEPA = { PlayerOutfitList.Select(x => (uint)x) } };
}

[MemoryPackable]
public partial class PlayerHeadFrameInfo
{
    /// <summary>
    ///     头像框有效期(秒级时间戳), 用于服务端没有赛季结束时间时代替"当期有效": 2099-12-31 12:00:00 UTC。
    ///     客户端把它当时间戳显示("限时头像框在#1前有效"), 且 xLua 只接受 2^53 以内的整数,
    ///     long.MaxValue 会让 HeadIconModifyPanel 抛 "beyond double int limits", 整个页面切换失败。
    /// </summary>
    public const long NoSeasonExpireTime = 4102401600;

    public long HeadFrameExpireTime { get; set; }
    public uint HeadFrameId { get; set; }

    public HeadFrameInfo ToProto()
    {
        return new HeadFrameInfo
        {
            // 旧存档写的是 long.MaxValue, 下发时收进客户端能表示的范围
            HeadFrameExpireTime = Math.Min(HeadFrameExpireTime, NoSeasonExpireTime),
            HeadFrameItemId = HeadFrameId
        };
    }
}

[MemoryPackable]
public partial class PrivacySettingsPb
{
    public bool DisplayChallengeLineup { get; set; } = true;
    public bool DisplayActiveState { get; set; } = true;
    public bool DisplayRecentlyState { get; set; } = true;
    public bool DisplayBattleRecord { get; set; } = true;
    public bool DisplayCollection { get; set; } = true;

    // 隐藏头像框(默认展示)。客户端 PlayerSettingData.IsDisplayHeadFrame = !该值:
    // 登录读 PlayerSettingInfo 字段 11, 切换发 UpdatePlayerSetting oneof 字段 1600。新成员只能追加在末尾(MemoryPack)。
    public bool HideHeadFrame { get; set; }

    public PlayerSettingInfo ToSettingProto()
    {
        return new PlayerSettingInfo
        {
            // TODO: DisplayActiveState, DisplayRecentlyState and DisplayBattleRecord are not sent. Their fields
            // have not been identified since 4.3 renumbered PlayerSettingInfo.
            GNFDDPEMMFN = DisplayChallengeLineup,
            FDHEPCKLCAM = DisplayCollection,
            BOMGNMDOGLM = HideHeadFrame
        };
    }
}
