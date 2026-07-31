using March7thHoney.Data;
using March7thHoney.Database.Avatar;
using March7thHoney.Enums.GridFight;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Lineup;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight.Battle;

public static class GridFightBattleProtoBuilder
{
    private const uint GridFightBattleMode = 2;

    private static readonly KeyValuePair<uint, uint>[] PropertyFixpointScales =
    [
        new(32, 180),
        new(33, 480),
        new(34, 440),
        new(44, 304),
        new(49, 180),
        new(52, 240),
        new(53, 480),
        new(57, 240),
        new(59, 450),
        new(1013, 100),
        new(1014, 100),
        new(1019, 100),
        new(1025, 100),
    ];

    public static List<AvatarLineupData> Populate(
        BattleInstance battle,
        GridFightSession session,
        GridFightResourceCatalog catalog,
        SceneBattleInfo proto)
    {
        var collection = new PlayerDataCollection(
            battle.Player.Data,
            battle.Player.InventoryManager!.Data,
            battle.Lineup);
        var avatars = BuildBattleAvatars(battle, session);
        var foregroundRoles = session.Roles
            .Where(role => role.Position is >= 1 and <= 4)
            .OrderBy(role => role.Position)
            .ToList();
        var battleRoles = new List<GridFightRoleState>(foregroundRoles.Count);
        foreach (var role in foregroundRoles)
        {
            var avatar = ResolveBattleAvatar(battle, role.RoleId, isForeground: true);
            if (avatar == null) continue;
            avatar.AvatarInfo.SetCurHp(10000, true);
            proto.BattleAvatarList.Add(avatar.AvatarInfo.ToBattleProto(collection, avatar.AvatarType));
            battleRoles.Add(role);
        }
        AddEntryBuffs(battle, battleRoles);

        proto.MonsterWaveList.Clear();
        var wave = new SceneMonsterWave
        {
            BattleStageId = session.CurrentNode.StageId,
            BattleWaveId = 1,
            MonsterParam = new SceneMonsterWaveParam
            {
                MonsterWorldLevel = (uint)Math.Max(1, battle.Player.Data.WorldLevel - 1),
                EliteGroup = session.Enemies.FirstOrDefault()?.Star1EliteGroup3 ?? 0,
            },
        };
        wave.MonsterList.Add(session.Enemies.Select(monster => new SceneMonster
        {
            MonsterId = monster.MonsterID,
            ExtraInfo = new GridFightSceneMonsterExtraInfo
            {
                GridFightInfo = new GridFightMonsterBattleInfo { RoleStar = 1 },
            },
        }));
        proto.MonsterWaveList.Add(wave);

        var activeRoles = session.Roles
            .Where(role => role.Position is >= 1 and <= 13)
            .OrderBy(role => role.Position)
            .ToList();
        var gridFightInfo = new GridFightInfo
        {
            BattleWaveId = 1,
            GridFightLineupHp = session.LineupHp,
            MaxActiveRoleCount = session.MaxActiveRoles,
            PenaltyBonusRuleId = session.CurrentNode.PenaltyBonusRuleId,
            EnemyDifficultyLevel = catalog.GetEnemyDifficultyLevel(session.DivisionId),
            Season = session.Season,
            DivisionId = session.DivisionId,
            ActiveRoleCount = (uint)activeRoles.Count,
            IsOverlock = session.IsOverlock,
            NodeInfo = new GridFightNodeInfo
            {
                RouteId = catalog.StandardRouteId,
                ChapterId = session.ChapterId,
                SectionId = session.SectionId,
                NodeType = (uint)session.CurrentNode.NodeType,
            },
        };
        if (session.SelectedPortalId != 0)
            gridFightInfo.GridFightPortalBuffList.Add(new GridFightPortalInfo
            {
                PortalBuffId = session.SelectedPortalId,
            });
        foreach (var role in activeRoles.Where(role => role.Position is >= 5 and <= 13))
        {
            var avatar = ResolveBattleAvatar(battle, role.RoleId, isForeground: false);
            if (avatar == null) continue;
            avatar.AvatarInfo.SetCurHp(10000, true);
            gridFightInfo.BackgroundAvatarList.Add(
                avatar.AvatarInfo.ToBattleProto(collection, avatar.AvatarType));
        }
        foreach (var role in activeRoles)
        {
            var basic = GameData.GridFightRoleBasicInfoData[role.RoleId];
            var roleInfo = new GridFightRoleInfo
            {
                RoleId = role.RoleId,
                AvatarId = basic.AvatarID,
                RoleStar = role.Star,
                Pos = role.Position,
                UniqueId = role.UniqueId,
                BattleExtInfo = new GridFightRoleBattleExtInfo
                {
                    ModeInfo = new GridFightRoleBattleModeInfo { Mode = GridFightBattleMode },
                    StateInfo = new GridFightRoleBattleStateInfo(),
                },
            };
            foreach (var scale in PropertyFixpointScales)
                roleInfo.ConvertPropertyToFixpoint.Add(scale.Key, scale.Value);
            gridFightInfo.GridGameRoleList.Add(roleInfo);
            foreach (var key in basic.RoleSavedValueList)
                gridFightInfo.RoleSavedValues[key] = 0;
        }
        foreach (var group in activeRoles
                     .SelectMany(role => GameData.GridFightRoleBasicInfoData[role.RoleId].TraitList
                         .Select(traitId => (TraitId: traitId, Role: role)))
                     .GroupBy(entry => entry.TraitId))
        {
            var count = (uint)group.Count();
            var activeLayer = GameData.GridFightTraitLayerData.GetValueOrDefault(group.Key)?.Keys
                .Where(layer => layer <= count)
                .DefaultIfEmpty()
                .Max() ?? 0;
            var traitInfo = new GridFightTraitInfo
            {
                TraitId = group.Key,
                Layer = activeLayer,
            };
            traitInfo.MemberList.Add(group.Select(entry => new GridFightTraitMemberInfo
            {
                MemberRoleUniqueId = entry.Role.UniqueId,
                MemberType = GridFightTraitMemberType.Role,
                MemberRoleId = entry.Role.RoleId,
                UpdateSource = GridFightUpdateSource.Role,
            }));
            if (activeLayer > 0)
                PopulateTraitEffects(traitInfo);
            gridFightInfo.GridFightTraitInfo.Add(traitInfo);
        }
        foreach (var role in activeRoles)
            if (GameData.GridFightRoleStarData.TryGetValue(role.RoleId << 4 | role.Star, out var roleStar) &&
                roleStar.BEID != 0)
                battle.BattleEvents.TryAdd((int)roleStar.BEID, new BattleEventInstance((int)roleStar.BEID, 0, 100000));
        foreach (var battleEventId in gridFightInfo.GridFightTraitInfo
                     .Select(info => GameData.GridFightTraitBasicInfoData.GetValueOrDefault(info.TraitId))
                     .Where(info => info != null)
                     .SelectMany(info => info!.BEIDList))
            battle.BattleEvents.TryAdd((int)battleEventId, new BattleEventInstance((int)battleEventId, 0, 100000));
        proto.BattleGridFightData = gridFightInfo;
        return avatars;
    }

    private static void AddEntryBuffs(BattleInstance battle, IReadOnlyList<GridFightRoleState> roles)
    {
        for (var ownerIndex = 0; ownerIndex < roles.Count; ownerIndex++)
        {
            var role = roles[ownerIndex];
            if (!TryResolveEntryMazeSkill(role, out var mazeSkillId)) continue;
            if (battle.Buffs.Any(buff => buff.BuffID == mazeSkillId && buff.OwnerIndex == ownerIndex)) continue;
            battle.Buffs.Add(new MazeBuff(mazeSkillId, 1, ownerIndex)
            {
                WaveFlag = -1,
                IncludeOwnerInTargetList = false,
            });
        }

        var leader = roles.FirstOrDefault();
        if (leader == null ||
            !GameData.GridFightRoleBasicInfoData.TryGetValue(leader.RoleId, out var basic) ||
            !GameData.AvatarConfigData.TryGetValue((int)basic.AvatarID, out var avatar) ||
            avatar.DamageType == 0)
            return;

        var buffId = (int)avatar.DamageType;
        if (battle.Buffs.Any(buff => buff.BuffID == buffId && buff.OwnerIndex == 0)) return;
        var skillIndex = GameData.GridFightRoleStarData.TryGetValue(leader.RoleId << 4 | leader.Star, out var roleStar) &&
                         !roleStar.SkillOverrideSrc.Contains(0)
            ? 2
            : 1;
        var buff = new MazeBuff(buffId, 1, 0)
        {
            WaveFlag = -1,
            IncludeOwnerInTargetList = false,
        };
        buff.DynamicValues["SkillIndex"] = skillIndex;
        battle.Buffs.Add(buff);
    }

    private static bool TryResolveEntryMazeSkill(GridFightRoleState role, out int mazeSkillId)
    {
        mazeSkillId = 0;
        if (!GameData.GridFightRoleBasicInfoData.TryGetValue(role.RoleId, out var basic)) return false;
        var adventurePlayerId = (int)basic.AvatarID;
        if (GameData.AvatarConfigData.TryGetValue(adventurePlayerId, out var avatar) && avatar.AdventurePlayerID != 0)
            adventurePlayerId = avatar.AdventurePlayerID;
        if (!GameData.AdventurePlayerData.TryGetValue(adventurePlayerId, out var adventure) ||
            adventure.MazeSkillIdList.Count == 0)
            return false;

        if (GameData.GridFightRoleStarData.TryGetValue(role.RoleId << 4 | role.Star, out var roleStar))
            mazeSkillId = adventure.MazeSkillIdList.FirstOrDefault(id => roleStar.SkillOverrideSrc.Contains((uint)id));
        if (mazeSkillId == 0) mazeSkillId = adventure.MazeSkillIdList[0];
        return mazeSkillId != 0;
    }

    public static List<AvatarLineupData> BuildBattleAvatars(BattleInstance battle, GridFightSession session)
    {
        var avatars = new List<AvatarLineupData>();
        foreach (var role in session.Roles
                     .Where(role => role.Position is >= 1 and <= 13)
                     .OrderBy(role => role.Position))
        {
            var avatar = ResolveBattleAvatar(battle, role.RoleId, isForeground: role.Position <= 4);
            if (avatar == null) continue;
            avatar.AvatarInfo.SetCurHp(10000, true);
            avatars.Add(avatar);
        }
        return avatars;
    }

    private static void PopulateTraitEffects(GridFightTraitInfo traitInfo)
    {
        if (!GameData.GridFightTraitBasicInfoData.TryGetValue(traitInfo.TraitId, out var trait)) return;
        foreach (var effectId in trait.TraitEffectList)
        {
            var effectInfo = new BattleGridFightTraitEffectInfo { EffectId = effectId };
            PopulateTraitBonusLevelInfo(effectInfo);
            traitInfo.TraitEffectList.Add(effectInfo);
        }
    }

    private static void PopulateTraitBonusLevelInfo(BattleGridFightTraitEffectInfo effectInfo)
    {
        if (!GameData.GridFightTraitEffectData.TryGetValue(effectInfo.EffectId, out var effect) ||
            effect.TraitEffectType != GridFightTraitEffectTypeEnum.TraitBonus ||
            !GameData.GridFightTraitBonusData.TryGetValue(effectInfo.EffectId, out var bonuses)) return;

        var levelInfo = new GridFightTraitEffectLevelInfo();
        foreach (var (threshold, bonus) in bonuses.OrderBy(entry => entry.Key))
        {
            var dropInfo = new GridFightDropInfo();
            foreach (var combinationId in bonus.BonusParamList)
            {
                if (!GameData.GridFightCombinationBonusData.TryGetValue(combinationId, out var combination)) continue;
                foreach (var poolId in combination.CombinationBonusList)
                {
                    if (!GameData.GridFightBasicBonusPoolV2Data.TryGetValue(poolId, out var pool)) continue;
                    if (pool.BonusType != GridFightBonusTypeEnum.Orb) continue;
                    var orbId = pool.BonusTypeParamList.FirstOrDefault();
                    if (orbId == 0) orbId = pool.BonusTypeParam;
                    if (orbId == 0) continue;
                    dropInfo.DropList.Add(new GridFightDropEntry
                    {
                        DropType = GridFightDropType.Orb,
                        ItemId = orbId,
                        Num = 1,
                    });
                }
            }
            if (dropInfo.DropList.Count > 0)
                levelInfo.TraitEffectLevelReward[threshold] = dropInfo;
        }
        if (levelInfo.TraitEffectLevelReward.Count > 0)
            effectInfo.TraitEffectLevelInfo = levelInfo;
    }

    private static AvatarLineupData? ResolveBattleAvatar(BattleInstance battle, uint roleId, bool isForeground)
    {
        if (!GameData.GridFightRoleBasicInfoData.TryGetValue(roleId, out var basic)) return null;
        var formal = battle.Player.AvatarManager!.GetFormalAvatar((int)basic.AvatarID);
        if (formal != null)
        {
            var avatarType = isForeground && string.Equals(basic.FrontBackType, "Back", StringComparison.Ordinal)
                ? AvatarType.AvatarFormalType
                : AvatarType.AvatarGridFightType;
            return new AvatarLineupData(formal, avatarType);
        }
        var trial = battle.Player.AvatarManager.GetTrialAvatarByWorldLevel(
            (int)basic.SpecialAvatarID,
            battle.Player.Data.WorldLevel);
        return trial == null ? null : new AvatarLineupData(trial, AvatarType.AvatarTrialType);
    }
}
