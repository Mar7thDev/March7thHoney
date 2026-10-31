using March7thHoney.Data;
using March7thHoney.Data.Config.AdventureAbility;
using March7thHoney.Data.Excel;
using March7thHoney.Data.Config.Task;
using March7thHoney.Database;
using March7thHoney.Database.Avatar;
using March7thHoney.Database.Lineup;
using March7thHoney.Enums.Avatar;
using March7thHoney.Enums.Quest;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.Lineup;
using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Proto;
using March7thHoney.Util;
using LineupInfo = March7thHoney.Database.Lineup.LineupInfo;

namespace March7thHoney.GameServer.Game.Lineup;

public class LineupManager : BasePlayerManager<LineupData>
{
    public LineupManager(PlayerInstance player) : base(player)
    {
        foreach (var lineupInfo in Data.Lineups.Values)
        {
            lineupInfo.LineupData = Data;
            lineupInfo.AvatarData = player.AvatarManager!.Data;
        }
        PadNormalLineups();
        EnsurePlayableCurLineup();
        RecalculateExtraMpCount();
    }

    #region Detail

    public LineupInfo? GetLineup(int lineupIndex)
    {
        Data.Lineups.TryGetValue(lineupIndex, out var lineup);
        return lineup;
    }

    public LineupInfo? GetExtraLineup(ExtraLineupType type)
    {
        var index = (int)type + 10;
        Data.Lineups.TryGetValue(index, out var lineup);
        return lineup;
    }

    public LineupInfo? GetCurLineup()
    {
        var lineup = GetLineup(Data.GetCurLineupIndex());
        return lineup;
    }

    public int GetMaxMp()
    {
        return 5 + Data.ExtraMpCount;
    }

    /// <summary>按当前编队重算 ExtraMpCount，加成来自角色 AdventureAbility 的 AdvModifyMaxMazeMP。</summary>
    public void RecalculateExtraMpCount()
    {
        var extra = 0;
        var lineup = GetCurLineup();
        foreach (var slot in lineup?.BaseAvatars ?? [])
            extra += ResolveExtraMazeMp(ResolveAvatarId(slot));

        Data.ExtraMpCount = extra;
        if (lineup != null)
            lineup.Mp = Math.Min(Math.Max(0, lineup.Mp), 5 + extra);
    }

    /// <summary>试用角色归一到本体，多命途角色归一到 BaseAvatarID。</summary>
    private static int ResolveAvatarId(LineupAvatarInfo slot)
    {
        var avatarId = slot.BaseAvatarId;
        if (slot.SpecialAvatarId > 0 &&
            GameData.SpecialAvatarData.TryGetValue(slot.SpecialAvatarId, out var special))
            avatarId = special.AvatarID;
        if (GameData.MultiplePathAvatarConfigData.TryGetValue(avatarId, out var multiPath))
            avatarId = multiPath.BaseAvatarID;
        return avatarId;
    }

    /// <summary>角色上场时 modifier 的 OnCreate 里声明的秘技点上限加成。</summary>
    private static int ResolveExtraMazeMp(int avatarId)
    {
        if (!GameData.AvatarConfigData.TryGetValue(avatarId, out var avatar)) return 0;
        if (!GameData.AdventureAbilityConfigListData.TryGetValue(avatar.AdventurePlayerID, out var abilities))
            return 0;

        var extra = 0;
        var modifiers = abilities.GlobalModifiers?.Values ?? Enumerable.Empty<AdventureModifierConfig>();
        foreach (var modifier in modifiers)
        foreach (var task in modifier.OnCreate)
        {
            if (task is not AdvModifyMaxMazeMP modify) continue;
            var value = modify.ModifyValue.GetValue();
            extra = modify.ModifyFunction == PropertyModifyFunctionEnum.Set ? value - 5 : extra + value;
        }
        return extra;
    }

    /// <summary>写入协议 LineupInfo.MaxMp，ToProto 可能漏带 ExtraMpCount 故发包前强制设置。</summary>
    public void ApplyMaxMp(Proto.LineupInfo proto)
    {
        if (proto == null) return;
        RecalculateExtraMpCount();
        proto.MaxMp = (uint)GetMaxMp();
        if (proto.Mp > proto.MaxMp)
            proto.Mp = proto.MaxMp;
    }

    public List<AvatarLineupData> GetAvatarsFromTeam(int index)
    {
        var lineup = GetLineup(index);
        if (lineup == null) return [];

        var avatarList = new List<AvatarLineupData>();
        foreach (var avatar in lineup.BaseAvatars!)
        {
            var avatarType = AvatarType.AvatarFormalType;
            BaseAvatarInfo? avatarInfo = null;
            if (avatar.SpecialAvatarId > 0)
            {
                avatarInfo = Player.AvatarManager!.GetTrialAvatar(avatar.SpecialAvatarId);
                avatarType = AvatarType.AvatarTrialType;
                // Trial protagonists can resolve to a different gender/base path than their table row.
                // Keep saved/replaced lineup slots consistent with the actual trial used in combat.
                if (avatarInfo != null && avatar.BaseAvatarId != avatarInfo.BaseAvatarId)
                {
                    if (lineup.LeaderAvatarId == avatar.BaseAvatarId) lineup.LeaderAvatarId = avatarInfo.BaseAvatarId;
                    avatar.BaseAvatarId = avatarInfo.BaseAvatarId;
                    MarkDirty();
                }
            }
            else if (avatar.AssistUid > 0)
            {
                var avatarStorage = DatabaseHelper.Instance?.GetInstance<AvatarData>(avatar.AssistUid);
                avatarType = AvatarType.AvatarAssistType;
                if (avatarStorage == null) continue;
                foreach (var avatarData in avatarStorage.FormalAvatars.Where(avatarData =>
                             avatarData.AvatarId == avatar.BaseAvatarId))
                {
                    avatarInfo = avatarData;
                    break;
                }
            }
            else
            {
                avatarInfo = Player.AvatarManager!.GetFormalAvatar(avatar.BaseAvatarId);
            }

            if (avatarInfo == null) continue;
            avatarList.Add(new AvatarLineupData(avatarInfo, avatarType));
        }

        return avatarList;
    }

    public List<AvatarLineupData> GetAvatarsFromCurTeam()
    {
        return GetAvatarsFromTeam(Data.GetCurLineupIndex());
    }

    /// <summary>普通队伍 0..8 按编号在前，额外队伍（键 &gt;= 10）按编号在后。</summary>
    public List<LineupInfo> GetAllLineup()
    {
        PadNormalLineups();
        return
        [
            .. Data.Lineups.Where(x => x.Key < GameConstants.MAX_LINEUP_COUNT).OrderBy(x => x.Key)
                .Select(x => x.Value),
            .. Data.Lineups.Where(x => x.Key >= GameConstants.MAX_LINEUP_COUNT).OrderBy(x => x.Key)
                .Select(x => x.Value)
        ];
    }

    /// <summary>
    ///     按编号补齐普通队伍槽 0..8。旧实现按字典条目数补齐，存档里缺中间编号或带额外队伍时会漏掉某些槽，
    ///     甚至因编号已存在而抛异常；队伍界面随之只剩一队（yuukisr_go 6bb4fd1 PadNormalLineups）。
    /// </summary>
    public void PadNormalLineups()
    {
        var changed = false;
        for (var i = 0; i < GameConstants.MAX_LINEUP_COUNT; i++)
        {
            if (Data.Lineups.TryGetValue(i, out var existing))
            {
                if (existing.BaseAvatars == null)
                {
                    existing.BaseAvatars = [];
                    changed = true;
                }
                continue;
            }

            changed = true;
            Data.Lineups[i] = new LineupInfo
            {
                Name = "",
                LineupType = 0,
                BaseAvatars = [],
                LineupData = Data,
                AvatarData = Player.AvatarManager!.Data
            };
        }
        if (changed) MarkDirty();
    }

    /// <summary>
    ///     当前普通队伍为空（存档指向从未保存过的槽、或补齐出来的空槽）时，改用编号最小的非空普通队伍，
    ///     避免以空队伍登录导致客户端建不出队长实体。没有任何非空队伍（新号）时保持不变。
    /// </summary>
    public void EnsurePlayableCurLineup()
    {
        if (Data.CurExtraLineup != -1) return;
        if (GetLineup(Data.CurLineup) is { BaseAvatars.Count: > 0 }) return;

        foreach (var (index, lineup) in Data.Lineups.Where(x => x.Key < GameConstants.MAX_LINEUP_COUNT)
                     .OrderBy(x => x.Key))
        {
            if (lineup.BaseAvatars is not { Count: > 0 }) continue;
            Data.CurLineup = index;
            MarkDirty();
            return;
        }
    }

    /// <summary>MarkPresetLineupCsReq：标记 / 取消常用队伍，只接受普通队伍编号。</summary>
    public bool SetLineupFavourite(int lineupIndex, bool isFavourite)
    {
        if (lineupIndex is < 0 or >= GameConstants.MAX_LINEUP_COUNT) return false;
        var lineup = GetLineup(lineupIndex);
        if (lineup == null) return false;

        lineup.IsFavourite = isFavourite;
        MarkDirty();
        return true;
    }

    #endregion

    #region Management

    public async ValueTask<bool> SetCurLineup(int lineupIndex)
    {
        if (lineupIndex < 0 || !Data.Lineups.ContainsKey(lineupIndex)) return false;
        if (GetLineup(lineupIndex)!.BaseAvatars!.Count == 0) return false;
        Data.CurLineup = lineupIndex;
        Data.CurExtraLineup = -1;

        Player.SceneInstance?.SyncLineup();
        await Player.SendPacket(new PacketSyncLineupNotify(GetCurLineup()!));

        return true;
    }

    public void SetExtraLineup(ExtraLineupType type, List<int> baseAvatarIds, bool refresh = false)
    {
        if (type == ExtraLineupType.LineupNone)
        {
            // reset lineup
            Data.CurExtraLineup = -1;
            return;
        }

        var index = (int)type + 10;

        // destroy old lineup
        Data.Lineups.Remove(index);

        // create new lineup
        var lineup = new LineupInfo
        {
            Name = "",
            LineupType = (int)type,
            BaseAvatars = [],
            LineupData = Data,
            AvatarData = Player.AvatarManager!.Data
        };

        var worldLevel = type == ExtraLineupType.LineupStageTrial ? 0 : Player.Data.WorldLevel;

        foreach (var avatarId in baseAvatarIds)
        {
            var trial = Player.AvatarManager!.GetTrialAvatar(avatarId, refresh);
            if (trial != null)
            {
                if (GameData.MultiplePathAvatarConfigData.TryGetValue(trial.AvatarId, out var pathExcel) &&
                    pathExcel.Gender != GenderTypeEnum.GENDER_NONE)
                    if (pathExcel.Gender != (GenderTypeEnum)Player.Data.CurrentGender)
                        continue;

                trial.CheckLevel(worldLevel);
                lineup.BaseAvatars!.Add(new LineupAvatarInfo
                    { BaseAvatarId = trial.BaseAvatarId, SpecialAvatarId = trial.SpecialAvatarId });
            }
            else
            {
                lineup.BaseAvatars!.Add(new LineupAvatarInfo { BaseAvatarId = avatarId });
            }
        }

        Data.Lineups.Add(index, lineup);
        Data.CurExtraLineup = index;
    }

    public async ValueTask SetExtraLineup(ExtraLineupType type, bool notify = true)
    {
        if (type == ExtraLineupType.LineupNone)
        {
            // reset lineup
            Data.CurExtraLineup = -1;
            if (notify) await Player.SendPacket(new PacketSyncLineupNotify(GetCurLineup()!));
            return;
        }

        var index = (int)type + 10;

        // get cur extra lineup
        var lineup = GetExtraLineup(type);
        if (lineup == null || lineup.BaseAvatars?.Count == 0) return;

        Data.CurExtraLineup = index;

        // sync
        if (notify) await Player.SendPacket(new PacketSyncLineupNotify(GetCurLineup()!));
    }

    public async ValueTask AddAvatar(int lineupIndex, int avatarId, bool sendPacket = true)
    {
        if (lineupIndex < 0) return;
        Data.Lineups.TryGetValue(lineupIndex, out var lineup);

        if (lineup == null)
        {
            var baseAvatarId = avatarId;
            if (GameData.MultiplePathAvatarConfigData.TryGetValue(baseAvatarId, out var multiPathAvatar))
                baseAvatarId = multiPathAvatar.BaseAvatarID;
            var specialAvatarId = avatarId * 10 + 0;
            GameData.SpecialAvatarData.TryGetValue(specialAvatarId, out var specialAvatar);
            if (specialAvatar != null)
            {
                Player.AvatarManager!.GetTrialAvatar(avatarId)?.CheckLevel(Player.Data.WorldLevel);
                baseAvatarId = specialAvatar.AvatarID;
            }
            else
            {
                // 仅当 avatarId 不是有效角色时才回退到男主，避免把女主/其他命途主角强制锁成 8001
                if (baseAvatarId > 8000 && !GameData.AvatarConfigData.ContainsKey(baseAvatarId))
                    baseAvatarId = 8001;
            }

            lineup = new LineupInfo
            {
                Name = "",
                LineupType = 0,
                BaseAvatars =
                [
                    new LineupAvatarInfo
                        { BaseAvatarId = baseAvatarId, SpecialAvatarId = specialAvatar?.SpecialAvatarID ?? 0 }
                ],
                LineupData = Data,
                AvatarData = Player.AvatarManager!.Data
            };
            Data.Lineups.Add(lineupIndex, lineup);
        }
        else
        {
            if (lineup.BaseAvatars!.Count >= 4) return;

            var baseAvatarId = avatarId;
            if (GameData.MultiplePathAvatarConfigData.TryGetValue(baseAvatarId, out var multiPathAvatar))
                baseAvatarId = multiPathAvatar.BaseAvatarID;
            var specialAvatarId = avatarId * 10 + 0;
            GameData.SpecialAvatarData.TryGetValue(specialAvatarId, out var specialAvatar);
            if (specialAvatar != null)
            {
                Player.AvatarManager!.GetTrialAvatar(avatarId)?.CheckLevel(Player.Data.WorldLevel);
                baseAvatarId = specialAvatar.AvatarID;
            }
            else
            {
                // 仅当 avatarId 不是有效角色时才回退到男主，避免把女主/其他命途主角强制锁成 8001
                if (baseAvatarId > 8000 && !GameData.AvatarConfigData.ContainsKey(baseAvatarId))
                    baseAvatarId = 8001;
            }

            lineup.BaseAvatars?.Add(new LineupAvatarInfo
                { BaseAvatarId = baseAvatarId, SpecialAvatarId = specialAvatar?.SpecialAvatarID ?? 0 });
            Data.Lineups[lineupIndex] = lineup;
        }

        MarkDirty();
        if (sendPacket)
        {
            if (lineupIndex == Data.GetCurLineupIndex())
            {
                RecalculateExtraMpCount();
                Player.SceneInstance?.SyncLineup();
            }
            await Player.SendPacket(new PacketSyncLineupNotify(lineup));
        }
    }

    public async ValueTask AddAvatarToCurTeam(int avatarId, bool sendPacket = true)
    {
        await AddAvatar(Data.GetCurLineupIndex(), avatarId, sendPacket);
    }

    // targets the real (non-virtual) lineup even while a story virtual lineup is still active
    public async ValueTask AddAvatarToRealTeam(int avatarId, bool sendPacket = true)
    {
        await AddAvatar(Data.CurLineup, avatarId, sendPacket);
    }

    public async ValueTask AddSpecialAvatarToCurTeam(int specialAvatarId, bool sendPacket = true)
    {
        Data.Lineups.TryGetValue(Data.GetCurLineupIndex(), out var lineup);
        GameData.SpecialAvatarData.TryGetValue(specialAvatarId, out var specialAvatar);
        if (specialAvatar == null) return;
        Player.AvatarManager!.GetTrialAvatar(specialAvatar.SpecialAvatarID)?.CheckLevel(Player.Data.WorldLevel);
        if (lineup == null)
        {
            lineup = new LineupInfo
            {
                Name = "",
                LineupType = 0,
                BaseAvatars =
                [
                    new LineupAvatarInfo
                        { BaseAvatarId = specialAvatar.AvatarID, SpecialAvatarId = specialAvatar.SpecialAvatarID }
                ],
                LineupData = Data,
                AvatarData = Player.AvatarManager!.Data
            };
            Data.Lineups.Add(Data.GetCurLineupIndex(), lineup);
        }
        else
        {
            if (lineup.BaseAvatars!.Count >= 4) lineup.BaseAvatars!.RemoveAt(3); // remove last avatar
            lineup.BaseAvatars?.Add(new LineupAvatarInfo
                { BaseAvatarId = specialAvatar.AvatarID, SpecialAvatarId = specialAvatar.SpecialAvatarID });
            Data.Lineups[Data.GetCurLineupIndex()] = lineup;
        }

        if (sendPacket)
        {
            Player.SceneInstance?.SyncLineup();
            await Player.SendPacket(new PacketSyncLineupNotify(lineup));
        }
    }

    public async ValueTask AddTrialAvatarToCurTeam(int specialAvatarId, bool sendPacket = true)
    {
        var lineup = GetCurLineup();
        if (lineup == null) return;
        var trial = Player.AvatarManager!.GetTrialAvatar(specialAvatarId);
        if (trial == null) return;
        if (lineup.BaseAvatars!.Any(a => a.SpecialAvatarId == trial.SpecialAvatarId)) return; // already joined
        trial.CheckLevel(Player.Data.WorldLevel);

        var slot = new LineupAvatarInfo
            { BaseAvatarId = trial.BaseAvatarId, SpecialAvatarId = trial.SpecialAvatarId };
        // A Trailblazer-path trial replaces the hero in their own slot. Appending it shifted the
        // party and opened the fight on the wrong character.
        var hero = IsTrailblazerPath(trial.AvatarId)
            ? lineup.BaseAvatars.FindIndex(IsOwnedTrailblazer)
            : -1;
        if (hero >= 0)
        {
            var wasLeader = lineup.LeaderAvatarId == lineup.BaseAvatars[hero].BaseAvatarId;
            lineup.BaseAvatars[hero] = slot;
            if (wasLeader) lineup.LeaderAvatarId = trial.BaseAvatarId;
        }
        else
        {
            if (lineup.BaseAvatars.Count >= 4) lineup.BaseAvatars.RemoveAt(lineup.BaseAvatars.Count - 1);
            lineup.BaseAvatars.Add(slot);
        }

        lineup.IsVirtual = true;
        if (lineup.PlaneId == 0) lineup.PlaneId = Player.Data.PlaneId;
        MarkDirty();

        if (!sendPacket) return;

        await Player.SendPacket(new PacketVirtualLineupTrialAvatarChangeScNotify(lineup.PlaneId,
            [trial.SpecialAvatarId], [], true));
        Player.SceneInstance?.SyncLineup();
        await Player.SendPacket(new PacketSyncLineupNotify(lineup));
    }

    /// <summary>
    ///     A bare CreateTrialPlayer beat names no avatar. The next Trailblazer path gated by this
    ///     mission (or its NextTrackMainMission) is granted when that path has a story-trial row
    ///     (1000000 + path id).
    /// </summary>
    public async ValueTask GrantBareStoryTrial(SubMissionData subMission)
    {
        var specialId = ResolveStoryTrialAvatar(subMission);
        if (specialId != 0)
            await AddTrialAvatarToCurTeam(specialId);
    }

    /// <summary>
    ///     Fixed-level trials resolve SceneEnterStage at world level 0. IsUseWorldLevel trials keep
    ///     the player's world level. Without the raw flag table this stays the player's world level.
    /// </summary>
    public int StageResolveWorldLevel()
    {
        if (!StoryTrialTables.HasSpecialAvatarFlags) return Player.Data.WorldLevel;
        foreach (var slot in GetCurLineup()?.BaseAvatars ?? [])
        {
            if (slot.SpecialAvatarId == 0) continue;
            if (StoryTrialTables.SpecialUsesWorldLevel(slot.SpecialAvatarId)) continue;
            return 0;
        }

        return Player.Data.WorldLevel;
    }

    private int ResolveStoryTrialAvatar(SubMissionData subMission)
    {
        if (subMission.MainMissionId == 0) return 0;
        HashSet<int> chain = [subMission.MainMissionId];
        if (GameData.MainMissionData.TryGetValue(subMission.MainMissionId, out var main) &&
            main.NextTrackMainMission != 0)
            chain.Add(main.NextTrackMainMission);

        foreach (var path in GameData.MultiplePathAvatarConfigData.Values.OrderBy(path => path.AvatarID))
        {
            if (path.BaseAvatarID != 8001 || path.UnlockConditions.Count == 0) continue;
            if (PlayerInstance.NormalizeHeroPathId(path.AvatarID, Player.Data.CurrentGender) != path.AvatarID)
                continue;
            if (!UnlockConditionInChain(path.UnlockConditions, chain)) continue;
            var specialId = 1_000_000 + path.AvatarID;
            // Rows are keyed SpecialAvatarID*10+WorldLevel; any world level counts.
            if (Enumerable.Range(0, 10).Any(level => GameData.SpecialAvatarData.ContainsKey(specialId * 10 + level)))
                return specialId;
        }

        return 0;
    }

    private static bool UnlockConditionInChain(List<Condition> conditions, HashSet<int> chain)
    {
        foreach (var condition in conditions)
        {
            if (!int.TryParse(condition.Param, out var id) || id == 0) continue;
            if (condition.Type == ConditionTypeEnum.FinishMainMission && chain.Contains(id)) return true;
            if (condition.Type == ConditionTypeEnum.FinishSubMission &&
                GameData.SubMissionInfoData.TryGetValue(id, out var sub) && chain.Contains(sub.MainMissionId))
                return true;
        }

        return false;
    }

    private static bool IsTrailblazerPath(int avatarId)
    {
        return GameData.MultiplePathAvatarConfigData.TryGetValue(avatarId, out var path) &&
               path.BaseAvatarID == 8001;
    }

    private static bool IsOwnedTrailblazer(LineupAvatarInfo slot)
    {
        return slot.SpecialAvatarId == 0 && (slot.BaseAvatarId is 8001 or 8002 || IsTrailblazerPath(slot.BaseAvatarId));
    }

    public async ValueTask RemoveTrialAvatarFromCurTeam(int specialAvatarId, bool sendPacket = true)
    {
        var lineup = GetCurLineup();
        if (lineup == null) return;
        if (lineup.BaseAvatars!.RemoveAll(a => a.SpecialAvatarId == specialAvatarId) == 0) return;

        var planeId = lineup.PlaneId != 0 ? lineup.PlaneId : Player.Data.PlaneId;
        var wasVirtual = lineup.IsVirtual;
        var lastTrial = !lineup.BaseAvatars.Any(avatar => avatar.SpecialAvatarId != 0);
        if (lastTrial && wasVirtual)
        {
            lineup.IsVirtual = false;
            lineup.PlaneId = 0;
        }

        // a virtual lineup only exists to host trial avatars; tear it down once the last one leaves,
        // even when the caller suppresses packets, so the saved state never keeps an empty extra lineup
        var tornDown = lineup.IsExtraLineup() && lineup.BaseAvatars!.Count == 0;
        if (tornDown)
        {
            Data.Lineups.Remove(Data.GetCurLineupIndex());
            Data.CurExtraLineup = -1;
        }

        MarkDirty();
        if (!sendPacket) return;

        if (wasVirtual)
            await Player.SendPacket(new PacketVirtualLineupTrialAvatarChangeScNotify(planeId, [],
                [specialAvatarId], false));
        if (lastTrial && wasVirtual && planeId != 0)
            await Player.SendPacket(new PacketVirtualLineupDestroyNotify(planeId));

        if (tornDown)
        {
            // tell the client the extra lineup is gone, then resync the scene against the restored lineup
            await Player.SendPacket(new PacketExtraLineupDestroyNotify((ExtraLineupType)lineup.LineupType));
            if (Player.SceneInstance != null) await Player.SceneInstance.SyncLineup();
            var restored = GetCurLineup();
            if (restored != null) await Player.SendPacket(new PacketSyncLineupNotify(restored));
        }
        else
        {
            if (Player.SceneInstance != null) await Player.SceneInstance.SyncLineup();
            await Player.SendPacket(new PacketSyncLineupNotify(lineup));
        }
    }

    public async ValueTask RemoveAvatar(int lineupIndex, int avatarId, bool sendPacket = true)
    {
        if (lineupIndex < 0) return;
        Data.Lineups.TryGetValue(lineupIndex, out var lineup);
        if (lineup == null) return;
        GameData.SpecialAvatarData.TryGetValue(avatarId * 10 + Player.Data.WorldLevel, out var specialAvatar);
        if (specialAvatar != null)
            lineup.BaseAvatars?.RemoveAll(avatar => avatar.BaseAvatarId == specialAvatar.AvatarID);
        else
            lineup.BaseAvatars?.RemoveAll(avatar => avatar.BaseAvatarId == avatarId);
        Data.Lineups[lineupIndex] = lineup;

        if (sendPacket)
        {
            if (lineupIndex == Data.GetCurLineupIndex())
            {
                RecalculateExtraMpCount();
                Player.SceneInstance?.SyncLineup();
            }
            await Player.SendPacket(new PacketSyncLineupNotify(lineup));
        }
    }

    public async ValueTask RemoveAvatarFromCurTeam(int avatarId, bool sendPacket = true)
    {
        await RemoveAvatar(Data.GetCurLineupIndex(), avatarId, sendPacket);
    }

    public async ValueTask ReplaceLineup(int lineupIndex, List<int> lineupSlotList,
        ExtraLineupType extraLineupType = ExtraLineupType.LineupNone)
    {
        if (extraLineupType != ExtraLineupType.LineupNone)
        {
            Data.CurExtraLineup = (int)extraLineupType + 10;
            if (!Data.Lineups.ContainsKey(Data.CurExtraLineup)) SetExtraLineup(extraLineupType, []);
        }

        LineupInfo lineup;
        if (Data.CurExtraLineup != -1)
            lineup = Data.Lineups[Data.CurExtraLineup]; // Extra lineup
        else if (lineupIndex < 0 || !Data.Lineups.TryGetValue(lineupIndex, out var dataLineup))
            return;
        else
            lineup = dataLineup;
        lineup.BaseAvatars = [];
        var index = lineup.LineupType == 0 ? lineupIndex : Data.GetCurLineupIndex();
        foreach (var avatar in lineupSlotList) await AddAvatar(index, avatar, false);
        lineup.LeaderAvatarId = lineup.BaseAvatars.FirstOrDefault()?.BaseAvatarId ?? 0;

        if (index == Data.GetCurLineupIndex())
        {
            RecalculateExtraMpCount();
            Player.SceneInstance?.SyncLineup();
        }
        await Player.SendPacket(new PacketSyncLineupNotify(lineup));
    }

    public async ValueTask ReplaceLineup(ReplaceLineupCsReq req)
    {
        if (req.ExtraLineupType != ExtraLineupType.LineupNone)
        {
            Data.CurExtraLineup = (int)req.ExtraLineupType + 10;
            if (!Data.Lineups.ContainsKey(Data.CurExtraLineup)) SetExtraLineup(req.ExtraLineupType, []);
        }

        LineupInfo lineup;
        if (Data.CurExtraLineup != -1)
            lineup = Data.Lineups[Data.CurExtraLineup]; // Extra lineup
        else if (!Data.Lineups.ContainsKey((int)req.Index))
            return;
        else
            lineup = Data.Lineups[(int)req.Index];
        lineup.BaseAvatars = [];
        var index = lineup.LineupType == 0 ? (int)req.Index : Data.GetCurLineupIndex();
        foreach (var avatar in req.LineupSlotList) await AddAvatar(index, (int)avatar.Id, false);
        lineup.LeaderAvatarId = lineup.BaseAvatars.FirstOrDefault()?.BaseAvatarId ?? 0;

        if (index == Data.GetCurLineupIndex())
        {
            RecalculateExtraMpCount();
            Player.SceneInstance?.SyncLineup();
        }
        await Player.SendPacket(new PacketSyncLineupNotify(lineup));
    }

    public async ValueTask DestroyExtraLineup(ExtraLineupType type)
    {
        var index = (int)type + 10;
        Data.Lineups.Remove(index);
        await Player.SendPacket(new PacketExtraLineupDestroyNotify(type));
    }

    public async ValueTask CostMp(int count, uint castEntityId = 1)
    {
        var curLineup = GetCurLineup()!;
        curLineup.Mp -= count;
        curLineup.Mp = Math.Min(Math.Max(0, curLineup.Mp), GetMaxMp());

        await Player.SendPacket(new PacketSceneCastSkillMpUpdateScNotify(castEntityId, curLineup.Mp));
    }

    public async ValueTask GainMp(int count, bool sendPacket = true,
        SyncLineupReason reason = SyncLineupReason.SyncReasonNone)
    {
        var curLineup = GetCurLineup()!;
        curLineup.Mp += count;
        curLineup.Mp = Math.Min(Math.Max(0, curLineup.Mp), GetMaxMp());
        if (sendPacket)
            await Player.SendPacket(
                new PacketSyncLineupNotify(GetCurLineup()!, reason));
    }

    #endregion
}

public record AvatarLineupData(BaseAvatarInfo AvatarInfo, AvatarType AvatarType, int? BattleAvatarId = null);
