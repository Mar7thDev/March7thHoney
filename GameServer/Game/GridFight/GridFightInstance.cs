using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.GameServer.Game.GridFight.Sync;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.GridFight.Battle;
using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

public class GridFightInstance(PlayerInstance player, uint season, uint divisionId, bool isOverLock, uint uniqueId)
{
    public PlayerInstance Player { get; } = player;
    public uint Season { get; } = season;
    public uint DivisionId { get; } = divisionId;
    public bool IsOverLock { get; } = isOverLock;
    public uint UniqueId { get; } = uniqueId;

    // 资源
    public uint Gold { get; set; } = 3;
    public uint LineupHp { get; set; } = 80;
    public uint LineupMaxHp { get; } = 100;
    public uint BattleMaxHp { get; } = 10939;
    public uint Level { get; } = 1;
    public uint SectionId { get; set; } = 1;
    public uint NDOCIKPLKIF { get; } = 1801;
    public uint CurrentChapterId { get; set; } = 1;
    public uint CurrentBranchId { get; set; } = 1;
    public uint KeepWinCnt { get; set; }
    public int BattlesFinished { get; set; }
    public GridFightBattleComponent BattleComponent { get; } = new();

    // 队列与唯一 ID 分配
    public uint QueuePosition { get; set; } = 1;
    public GridFightPendingAction? PendingAction { get; set; }
    public uint NextRoleUniqueId { get; set; } = 100;
    public uint NextEquipUniqueId { get; set; } = 50;
    public uint NextOrbUniqueId { get; set; } = 10;

    // 角色阵容
    public Dictionary<uint, uint> RoleByUniqueId { get; } = new();
    public Dictionary<uint, uint> UniqueIdByPos { get; } = new();
    public Dictionary<uint, uint> RoleStarByUniqueId { get; } = new();

    // 商店
    public uint ShopRefreshLeft { get; set; } = 2;
    public List<GridFightShopGoodsInfo> ShopGoods { get; } = new();
    public List<uint> ShopRolePool { get; } = new()
    {
        1217, 1314, 1108, 1220, 1406, 1001, 1009, 1306, 1403, 1502, 1408, 1321, 1409, 1402
    };

    // 装备草稿与背包
    public List<uint> StageCandidates { get; } = new() { 35030205, 35030405, 35030208, 350202, 35030606 };
    public List<uint> CurrentEquipDraft { get; private set; } = new() { 35030205u, 35030405u, 35030208u };
    public List<GridFightEquipmentInfo> Equipments { get; } = new();
    public List<GridFightConsumableInfo> Consumables { get; } = new();

    // 区段固定数据（增益、关卡）
    public List<uint> SectionAffixIds { get; private set; } = new();
    public List<uint> SessionCampIds { get; private set; } = new();
    public List<uint> ActivePortalBuffIds { get; } = new() { 106 };
    public List<uint> AvailablePortalBuffRerolls { get; } = new() { 105, 1014, 132 };
    public List<uint> CurrentPortalBuffOffer { get; private set; } = new();

    private static readonly uint[] FallbackPortalBuffPool =
    {
        101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115,
        116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 127, 129, 134, 135, 138,
        147, 1001, 1002, 1003, 1004, 1005, 1007, 1008, 1010, 1014, 1016,
        1101, 1102, 1104, 1106, 1107, 1108, 1112, 1113, 1114, 1115, 1116, 1118
    };

    public List<uint> RollPortalBuffs(int count = 3, IEnumerable<uint>? exclude = null)
    {
        var pool = (GameData.GridFightSeasonPortalData.TryGetValue(Season, out var seasonPool) && seasonPool.Count > 0)
            ? seasonPool.ToList()
            : FallbackPortalBuffPool.ToList();
        if (exclude != null)
            foreach (var id in exclude) pool.Remove(id);
        if (pool.Count <= count)
        {
            var fallback = pool.Distinct().ToList();
            while (fallback.Count < count)
                fallback.Add(FallbackPortalBuffPool[fallback.Count % FallbackPortalBuffPool.Length]);
            CurrentPortalBuffOffer = fallback.Take(count).ToList();
            return CurrentPortalBuffOffer;
        }
        var rng = Random.Shared;
        var picked = new List<uint>(count);
        while (picked.Count < count)
        {
            var idx = rng.Next(pool.Count);
            picked.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        CurrentPortalBuffOffer = picked;
        return CurrentPortalBuffOffer;
    }

    public List<uint> EnsurePortalBuffOffer()
    {
        if (CurrentPortalBuffOffer.Count == 0) RollPortalBuffs();
        return CurrentPortalBuffOffer;
    }

    public void ClearPortalBuffOffer() => CurrentPortalBuffOffer = new List<uint>();

    // 初始 4 名 bench 角色（PortalBuff 确认后从池子里随机抽取，实例化到 pos 14-17）
    public List<(uint AvatarId, uint Pos, uint UniqueId, string Component)> RolledBenchRoles { get; private set; }
        = new();

    public void EnsureDefaultRoles()
    {
        // 抓包确认：StartGamePlay 后初始团队为空，角色在 PortalBuff 确认时通过 sync 实例化
        if (ShopGoods.Count == 0) RotateShop();
        EnsureSessionPreview();
    }

    public void EnsureSessionPreview()
    {
        if (SessionCampIds.Count == 0) SessionCampIds = RollSessionCamps();
        if (SectionAffixIds.Count == 0) SectionAffixIds = RollSessionAffixes();
    }

    private List<uint> RollSessionCamps(int count = 3)
    {
        var pool = GameData.GridFightCampData.Values
            .Where(c => c.SeasonID == Season || c.SeasonID == 0)
            .Select(c => c.ID)
            .Distinct()
            .ToList();
        if (pool.Count == 0)
            pool = new List<uint> { 1, 2, 4, 5, 6, 8, 10, 11, 12, 13, 15, 16, 17, 23, 24, 25, 26, 27, 28 };
        return PickDistinct(pool, count);
    }

    private List<uint> RollSessionAffixes()
    {
        var totalCount = 3;
        if (GameData.GridFightDivisionStageData.TryGetValue(DivisionId, out var stage)
            && stage.AffixChooseNumList.Count > 0)
        {
            totalCount = (int)stage.AffixChooseNumList.Sum(x => x);
        }
        if (totalCount <= 0) return new List<uint>();
        var pool = GameData.GridFightAffixConfigData.Keys.ToList();
        if (pool.Count == 0)
            pool = new List<uint> { 1001, 1002, 1003, 1004, 1005, 2002, 2003, 2004, 2005, 2006, 3001, 3002, 3003, 3004, 3005, 3006, 3007, 3008, 4001, 4002, 4003, 4005, 4006, 4007, 4008, 4009, 4010, 4011, 4012, 4013 };
        return PickDistinct(pool, totalCount);
    }

    private static List<uint> PickDistinct(List<uint> pool, int count)
    {
        var rng = Random.Shared;
        var copy = pool.ToList();
        var picked = new List<uint>(count);
        while (picked.Count < count && copy.Count > 0)
        {
            var idx = rng.Next(copy.Count);
            picked.Add(copy[idx]);
            copy.RemoveAt(idx);
        }
        while (picked.Count < count) picked.Add(pool[picked.Count % Math.Max(1, pool.Count)]);
        return picked;
    }

    // 在 PortalBuffAction 确认后调用，把 4 名 bench 角色加入实例状态
    // requiredTrait > 0 时，至少 1 名角色必须带这个 trait（让选了的星徽能穿上）
    public void MaterializeInitialBenchTeam(uint requiredTrait = 0)
    {
        if (RoleByUniqueId.Count > 0) return;
        if (RolledBenchRoles.Count == 0
            || (requiredTrait > 0 && !RolledBenchRoles.Any(r => RoleHasTrait(r.AvatarId, requiredTrait))))
        {
            RollInitialBenchRoles(requiredTrait: requiredTrait);
        }
        foreach (var (avatarId, pos, uniqueId, _) in RolledBenchRoles)
        {
            RoleByUniqueId[uniqueId] = avatarId;
            RoleStarByUniqueId[uniqueId] = 1;
            UniqueIdByPos[pos] = uniqueId;
        }
    }

    private static bool RoleHasTrait(uint avatarId, uint trait) =>
        GameData.GridFightRoleBasicInfoData.Values
            .Any(r => r.AvatarID == avatarId && r.TraitList.Contains(trait));

    public List<(uint AvatarId, uint Pos, uint UniqueId, string Component)> RollInitialBenchRoles(int count = 4, uint requiredTrait = 0)
    {
        var pool = GameData.GridFightRoleBasicInfoData.Values
            .Where(r => r.IsInPool && (r.SeasonID == Season || r.SeasonID == 0))
            .Where(r => r.AvatarID >= 1000 && r.AvatarID < 2000)
            .Where(r => r.RoleSavedValueList.Count > 0)
            .ToList();

        var rng = Random.Shared;
        var copy = pool.ToList();
        var picked = new List<GridFightRoleBasicInfoExcel>(count);

        // 抓包确认：官服在选了带 trait 的 buff 后，必至少 1 名 bench 角色带这个 trait，否则星徽穿不上、客户端不前进
        if (requiredTrait > 0)
        {
            var traitMatched = copy.Where(r => r.TraitList.Contains(requiredTrait)).ToList();
            if (traitMatched.Count > 0)
            {
                var pivot = traitMatched[rng.Next(traitMatched.Count)];
                picked.Add(pivot);
                copy.Remove(pivot);
            }
        }

        while (picked.Count < count && copy.Count > 0)
        {
            var idx = rng.Next(copy.Count);
            picked.Add(copy[idx]);
            copy.RemoveAt(idx);
        }

        RolledBenchRoles = new List<(uint, uint, uint, string)>(picked.Count);
        for (var i = 0; i < picked.Count; i++)
        {
            var role = picked[i];
            RolledBenchRoles.Add((role.AvatarID, (uint)(14 + i), AllocRoleUniqueId(),
                role.RoleSavedValueList[0]));
        }
        return RolledBenchRoles;
    }


    public uint AllocRoleUniqueId() => ++NextRoleUniqueId;
    public uint AllocEquipUniqueId() => ++NextEquipUniqueId;
    public uint AllocOrbUniqueId() => ++NextOrbUniqueId;

    // 结算快照
    public uint PreBattleLineupHp { get; set; }
    public uint PreBattleLevel { get; set; }
    public uint PreBattleExp { get; set; }
    public uint PlayerLevel { get; set; } = 2;
    public uint PlayerExp { get; set; }
    public uint PlayerMaxLevel { get; } = 10;
    public uint LevelUpExpNeed { get; } = 4;
    public uint CampId { get; set; } = 18;
    public uint PreBattleGold { get; set; }
    public Proto.GridFightDamageSttInfo? LastBattleDamageStt { get; set; }
    public List<(uint ItemId, uint UniqueId)> LastRewardedOrbs { get; } = new();
    public List<Proto.GridFightHpModifyInfo> LastHpModifyTimeline { get; } = new();
    public Dictionary<uint, Proto.GridFightDropInfo> LastSectionRewards { get; } = new();

    public void AdvanceQueue(uint by = 1)
    {
        QueuePosition += by;
        PendingAction = null;
    }

    public void ApplyPositionList(IEnumerable<GridFightPosInfo> posInfoList)
    {
        EnsureDefaultRoles();
        foreach (var posInfo in posInfoList)
        {
            if (posInfo.Pos == 0) continue;
            foreach (var stale in UniqueIdByPos
                         .Where(kv => kv.Key != posInfo.Pos && kv.Value == posInfo.UniqueId)
                         .Select(kv => kv.Key).ToList())
                UniqueIdByPos.Remove(stale);
            UniqueIdByPos[posInfo.Pos] = posInfo.UniqueId;
            if (posInfo.UniqueId != 0 && !RoleByUniqueId.ContainsKey(posInfo.UniqueId))
            {
                RoleByUniqueId[posInfo.UniqueId] = 8001;
                RoleStarByUniqueId[posInfo.UniqueId] = 1;
            }
        }
    }

    public List<(uint RoleId, uint Pos)> ResolveForegroundRoles()
    {
        EnsureDefaultRoles();
        return UniqueIdByPos.OrderBy(kv => kv.Key)
            .Where(kv => kv.Key is > 0 and <= 4)
            .Select(kv => (Role: RoleByUniqueId.GetValueOrDefault(kv.Value), Pos: kv.Key))
            .Where(t => t.Role != 0)
            .Select(t => (RoleId: t.Role, t.Pos))
            .ToList();
    }

    public List<(uint RoleId, uint Pos)> ResolveBackgroundRoles()
    {
        EnsureDefaultRoles();
        return UniqueIdByPos.OrderBy(kv => kv.Key)
            .Where(kv => kv.Key is > 4 and <= 13)
            .Select(kv => (Role: RoleByUniqueId.GetValueOrDefault(kv.Value), Pos: kv.Key))
            .Where(t => t.Role != 0)
            .Select(t => (RoleId: t.Role, t.Pos))
            .ToList();
    }

    public List<int> BuildForegroundAvatarIds(int maxCount = 4)
    {
        var list = ResolveForegroundRoles().Select(t => RoleIdToAvatarId(t.RoleId)).Where(id => id != 0).Take(maxCount).ToList();
        if (list.Count == 0)
            list = RoleByUniqueId.Values.Take(maxCount).Select(x => (int)x).ToList();
        return list;
    }

    public List<int> BuildBackgroundAvatarIds()
    {
        return ResolveBackgroundRoles().Select(t => RoleIdToAvatarId(t.RoleId)).Where(id => id != 0).ToList();
    }

    private static int RoleIdToAvatarId(uint roleId) =>
        GameData.GridFightRoleBasicInfoData.TryGetValue(roleId, out var e) ? (int)e.AvatarID : (int)roleId;

    public List<IENNMHMOONM> CheckTrait()
    {
        EnsureDefaultRoles();
        var counts = new Dictionary<uint, uint>();
        var memberMap = new Dictionary<uint, List<(uint AvatarId, uint UniqueId)>>();

        foreach (var kv in UniqueIdByPos.Where(kv => kv.Key is > 0 and <= 13))
        {
            var uniqueId = kv.Value;
            if (uniqueId == 0 || !RoleByUniqueId.TryGetValue(uniqueId, out var roleId)) continue;
            if (!GameData.GridFightRoleBasicInfoData.TryGetValue(roleId, out var role)) continue;
            foreach (var tid in role.TraitList)
            {
                counts[tid] = counts.GetValueOrDefault(tid) + 1;
                if (!memberMap.TryGetValue(tid, out var list))
                    memberMap[tid] = list = new List<(uint, uint)>();
                list.Add((role.AvatarID, uniqueId));
            }
        }

        var result = new List<IENNMHMOONM>();
        foreach (var (tid, count) in counts)
        {
            if (count == 0) continue;
            if (!GameData.GridFightTraitBasicInfoData.TryGetValue(tid, out var traitExcel)) continue;

            uint layer = 0;
            if (GameData.GridFightTraitLayerData.TryGetValue(tid, out var layerMap) && layerMap.Count > 0)
            {
                foreach (var (threshold, excel) in layerMap)
                    if (threshold <= count && excel.Layer > layer)
                        layer = excel.Layer;
            }

            var trait = new IENNMHMOONM { TraitId = tid, NKFDBEHPNLG = layer };
            if (memberMap.TryGetValue(tid, out var members))
            {
                foreach (var (avatarId, uniqueId) in members)
                {
                    trait.MEEPFKLLIJB.Add(new BGNGLHHBGMI
                    {
                        EIHHLAOKAPH = GAPBBJCLMGP.Dgecgaafdjm,
                        GDNIKJGAEBH = avatarId,
                        IPDCMHIEKIJ = uniqueId,
                        GridUpdateSrc = PFODGDGFBBN.Iomeeecoiob
                    });
                }
            }
            if (layer > 0)
            {
                foreach (var effectId in traitExcel.TraitEffectList)
                    trait.TraitEffectList.Add(new BattleGridFightTraitEffectInfo { EffectId = effectId });
            }
            result.Add(trait);
        }
        return result;
    }

    public void RotateShop()
    {
        ShopGoods.Clear();
        if (ShopRolePool.Count == 0) return;
        for (var i = 0; i < 5; i++)
        {
            var roleId = ShopRolePool[(BattlesFinished * 5 + i) % ShopRolePool.Count];
            ShopGoods.Add(new GridFightShopGoodsInfo
            {
                ShopGoodsPrice = 1,
                RoleGoodsInfo = new GridFightRoleGoodsInfo { RoleId = roleId, RoleStar = 1 }
            });
        }
    }

    public void RefreshEquipDraft()
    {
        var startIdx = (BattlesFinished * 3) % StageCandidates.Count;
        CurrentEquipDraft = new List<uint>
        {
            StageCandidates[startIdx % StageCandidates.Count],
            StageCandidates[(startIdx + 1) % StageCandidates.Count],
            StageCandidates[(startIdx + 2) % StageCandidates.Count]
        };
    }

    public (bool ok, uint addedUniqueId, uint roleId, uint pos) TryBuyGoods(int shopIndex)
    {
        if (shopIndex < 0 || shopIndex >= ShopGoods.Count) return (false, 0, 0, 0);
        var goods = ShopGoods[shopIndex];
        if (goods.RoleGoodsInfo == null || goods.IsSoldOut) return (false, 0, 0, 0);
        var price = goods.ShopGoodsPrice;
        if (Gold < price) Gold = price; // 私服宽松：金币不足时仍发买单
        Gold -= price;
        goods.IsSoldOut = true;
        var roleId = goods.RoleGoodsInfo.RoleId;
        var uniqueId = AllocRoleUniqueId();
        RoleByUniqueId[uniqueId] = roleId;
        RoleStarByUniqueId[uniqueId] = 1;
        // 找一个空闲位置（候补区 14-18，再退到 6-13）
        uint pos = 0;
        foreach (var candidate in new uint[] { 14, 15, 16, 17, 18, 6, 7, 8, 9, 10, 11, 12, 13 })
        {
            if (!UniqueIdByPos.ContainsKey(candidate))
            {
                pos = candidate;
                break;
            }
        }
        if (pos > 0) UniqueIdByPos[pos] = uniqueId;
        return (true, uniqueId, roleId, pos);
    }

    public (bool ok, uint refund) TryRecycleRole(uint uniqueId)
    {
        if (!RoleByUniqueId.Remove(uniqueId)) return (false, 0);
        RoleStarByUniqueId.Remove(uniqueId);
        foreach (var pos in UniqueIdByPos.Where(kv => kv.Value == uniqueId).Select(kv => kv.Key).ToList())
            UniqueIdByPos.Remove(pos);
        var refund = 1u;
        Gold += refund;
        return (true, refund);
    }

    public bool TryRefreshShop()
    {
        // 抓包：GLIFNMBMMBL=2 是刷新单价，不是次数；金币不够也宽松放行
        var cost = ShopRefreshLeft == 0 ? 2u : ShopRefreshLeft;
        Gold = Gold < cost ? 0 : Gold - cost;
        RotateShop();
        return true;
    }

    public bool TryBuyExp()
    {
        Gold = Gold < 4 ? 0 : Gold - 4;
        return true;
    }

    public bool TryEquipFromDraft(uint idx)
    {
        if (idx >= CurrentEquipDraft.Count) return false;
        var equipId = CurrentEquipDraft[(int)idx];
        Equipments.Add(new GridFightEquipmentInfo
        {
            GridFightEquipmentId = equipId,
            Source = 1,
            UniqueId = AllocEquipUniqueId()
        });
        return true;
    }

    public void OnBattleResolved(bool win)
    {
        BattlesFinished++;
        if (win)
        {
            KeepWinCnt++;
            SectionId++;
        }
        ShopRefreshLeft = 2;
        RotateShop();
        RefreshEquipDraft();
    }

    private static readonly uint[] Section1RewardOrbs = { 102u, 199u, 203u, 204u };

    public void RecordBattleSnapshot(PVEBattleResultCsReq req, bool win)
    {
        PreBattleLineupHp = LineupHp;
        PreBattleLevel = PlayerLevel;
        PreBattleExp = PlayerExp;
        PreBattleGold = Gold;

        LastBattleDamageStt = new GridFightDamageSttInfo();
        var activeRoleIds = new HashSet<uint>();
        if (req.Stt?.OGPOFMOGPIP?.JPDJMCCKENI != null)
        {
            foreach (var entry in req.Stt.OGPOFMOGPIP.JPDJMCCKENI)
            {
                var roleId = entry.RoleId;
                if (roleId == 0) continue;
                var inActive = entry.Damage > 0 || entry.BOIEGPAPHOP > 0;
                if (inActive) activeRoleIds.Add(roleId);
                LastBattleDamageStt.EABPCKEDDBH.Add(new HHHMMJBGCNG
                {
                    RoleId = roleId,
                    RoleStar = 1,
                    TotalDamage = entry.Damage,
                    LDMNBDIDFCC = inActive,
                    HNLEDBPGDBC = !inActive
                });
            }
        }

        LastRewardedOrbs.Clear();
        LastSectionRewards.Clear();
        LastHpModifyTimeline.Clear();

        if (!win) return;

        // 1-1 通关固定奖励：金币 +3 基础 +1 连胜，4 颗晶矿
        Gold += 3;
        Gold += 1;
        var dropInfo = new GridFightDropInfo();
        foreach (var orbId in Section1RewardOrbs)
        {
            var orbUniqueId = AllocOrbUniqueId();
            LastRewardedOrbs.Add((orbId, orbUniqueId));
            dropInfo.PIBLJLBCKJL.Add(new LHPPIAKKFME
            {
                BGKDAMDFFKH = GridFightDropType.HiolcnpoponMkppcdpchie,
                JJFFLMCCCMM = orbId,
                Num = 1
            });
        }
        LastSectionRewards[2u] = dropInfo;

        PlayerExp += 2;
        if (PlayerExp >= LevelUpExpNeed && PlayerLevel < PlayerMaxLevel)
        {
            PlayerExp -= LevelUpExpNeed;
            PlayerLevel++;
        }

        // 完美通关 +2 生命值
        var perfectClear = req.Stt?.OGPOFMOGPIP?.GMOBFEBBFIE >= 0; // 无阵亡视为完美
        if (req.EndStatus == BattleEndStatus.BattleEndWin && perfectClear)
        {
            LineupHp = Math.Min(LineupMaxHp, LineupHp + 2);
        }

        // 5 段 HP 变化时间线，最后一段标记 +2
        var hpBefore = PreBattleLineupHp;
        foreach (var reason in new[]
        {
            GridFightUpdateGlobalHpReason.BakggpnhnneAhikonfebmj,
            GridFightUpdateGlobalHpReason.BakggpnhnneIkhlilhaiol,
            GridFightUpdateGlobalHpReason.BakggpnhnnePonicjjabek,
            GridFightUpdateGlobalHpReason.BakggpnhnneGpdhibafdcg
        })
        {
            LastHpModifyTimeline.Add(new GridFightHpModifyInfo
            {
                Reason = reason,
                FGEDKOINMAG = (int)hpBefore,
                EHMKLNEKIOE = (int)hpBefore
            });
        }
        LastHpModifyTimeline.Add(new GridFightHpModifyInfo
        {
            Reason = GridFightUpdateGlobalHpReason.BakggpnhnneGfimnccfkik,
            FGEDKOINMAG = (int)PreBattleLineupHp,
            EHMKLNEKIOE = (int)LineupHp,
            HPOPDNGCALL = LineupHp - PreBattleLineupHp,
            PDEKDHPNCEN = 1081
        });
    }

    public GridFightSeasonHandBookNotify BuildHandbookNotifyForBattle()
    {
        var n = new GridFightSeasonHandBookNotify
        {
            HandbookGridFightPortalInfo = new GridFightHandBookPortalInfo()
        };
        n.HandbookGridFightPortalInfo.GridFightPortalBuffList.Add(132u);
        return n;
    }

    public GridFightEndBattleStageNotify BuildEndBattleNotify(bool win)
    {
        var notify = new GridFightEndBattleStageNotify
        {
            BCOLJFHDLLD = LineupHp,
            FGEDKOINMAG = PreBattleLineupHp,
            IJEIEJLPGGJ = LineupMaxHp,
            KDOINLGKMBI = LineupMaxHp,
            DCPKPNLKGMM = Level,
            NDOCIKPLKIF = NDOCIKPLKIF,
            SectionId = SectionId,
            EJEIBPEKHLD = PlayerLevel,
            PNOJLNNHBIH = 1,
            HPOACJCPJHN = win,
            GCBBEEGANEG = new MIOMFOAEHEC
            {
                IPHHGMECAKB = new BLKAIEHOCBC
                {
                    Level = PlayerLevel,
                    Exp = PlayerExp,
                    DKHKEJIAJBN = LevelUpExpNeed
                },
                IJAEBDCFEMG = new BLKAIEHOCBC
                {
                    Level = PlayerLevel,
                    DKHKEJIAJBN = LevelUpExpNeed
                },
                MAGCGPMHHEA = 2
            }
        };

        if (LastBattleDamageStt != null)
            notify.GridFightDamageSttInfo = LastBattleDamageStt.Clone();

        foreach (var (sectionIdx, drop) in LastSectionRewards)
            notify.DHMBKAPKJFN[sectionIdx] = drop.Clone();

        foreach (var hp in LastHpModifyTimeline)
            notify.NNFAFGCGMBB.Add(hp.Clone());

        return notify;
    }

    public BattleInstance? StartBattle()
    {
        return GridFightBattleModule.StartBattle(Player, this);
    }

    public void ConfigureNextBattle(uint stageId, IEnumerable<uint> monsterIds)
    {
        BattleComponent.SetEncounter(stageId, monsterIds);
    }

    public async ValueTask EndBattle(BattleInstance battle, PVEBattleResultCsReq req)
    {
        var win = req.EndStatus != BattleEndStatus.BattleEndLose;
        RecordBattleSnapshot(req, win);
        AdvanceQueue(7);
        PendingAction = new GridFightPendingAction
        {
            QueuePosition = QueuePosition,
            RoundBeginAction = new GridFightRoundBeginActionInfo()
        };

        await Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(Player, GridFightSyncKind.PreSettle));
        await Player.SendPacket(new PacketGridFightEndBattleStageNotify(this, win));
        await Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(Player, GridFightSyncKind.PostBattle));

        OnBattleResolved(win);
    }

    public GridFightCurrentInfo ToProto()
    {
        EnsureDefaultRoles();
        if (CurrentEquipDraft.Count == 0) RefreshEquipDraft();
        if (ShopGoods.Count == 0) RotateShop();

        var info = new GridFightCurrentInfo
        {
            Season = Season,
            DivisionId = DivisionId,
            IsOverlock = IsOverLock,
            UniqueId = UniqueId,
            PendingAction = PendingAction ?? new GridFightPendingAction
            {
                QueuePosition = QueuePosition,
                PortalBuffAction = new GridFightPortalBuffActionInfo
                {
                    FCHPJKAIBHB = 1,
                    GridFightPortalBuffList = { EnsurePortalBuffOffer() }
                }
            },
            BCHPAOCOHIL = new FCBEHGJBJCN { AMNJHJJMPJF = { CurrentEquipDraft } },
            GridFightGameData = new GridFightGameData
            {
                PGFMICHMHFC =
                {
                    new GridFightGameItemInfo { UniqueId = 4, LMGLPGNACLP = new DCGINOONGLH() },
                    new GridFightGameItemInfo
                    {
                        UniqueId = 8,
                        MDFGMBJONAM = new HJJHNNDAEPF
                        {
                            KIBHCHLKGGC =
                            {
                                [1] = 2, [2] = 2, [3] = 4, [4] = 6, [5] = 20,
                                [6] = 40, [7] = 52, [8] = 72, [9] = 84, [10] = 0
                            }
                        }
                    },
                    new GridFightGameItemInfo { UniqueId = 9 },
                    new GridFightGameItemInfo { UniqueId = 12, ONJFAJOAFOG = new KPNJMMPJDKG() },
                    new GridFightGameItemInfo { UniqueId = 49, CJPMGPEIFGG = new BFBNMHEFKIG { CGDLGFHOECL = 2 } },
                    new GridFightGameItemInfo { UniqueId = 87, CJPMGPEIFGG = new BFBNMHEFKIG { CGDLGFHOECL = 1 } }
                }
            }
        };

        info.RogueCurrentGameInfo.Add(BuildBasicInfoSection());
        info.RogueCurrentGameInfo.Add(BuildTeamSection());
        info.RogueCurrentGameInfo.Add(BuildDraftSection());
        info.RogueCurrentGameInfo.Add(BuildItemsShopSection());
        info.RogueCurrentGameInfo.Add(BuildOrbSection());
        info.RogueCurrentGameInfo.Add(BuildLevelSection());
        info.RogueCurrentGameInfo.Add(new GridFightGameInfo { GridOrbInfo = new GridFightGameAugmentSync() });
        info.RogueCurrentGameInfo.Add(BuildAugmentSection());
        info.RogueCurrentGameInfo.Add(BuildTeamGameInfoSection());
        return info;
    }

    private GridFightGameInfo BuildBasicInfoSection() => new()
    {
        GridBasicInfo = new GridFightGameBasicInfo
        {
            ANBBPPHBCJH = 3,
            FLEJPPKLJIC = 3,
            HAEOPKELNEO = 10,
            Gold = Gold,
            GridFightBuyExpCost = 4,
            GridFightLineupHp = LineupHp,
            GridFightLineupMaxHp = LineupMaxHp,
            GridFightMaxAvatarCount = 9,
            GridFightMaxInterestGold = 5,
            GridFightOffFieldMaxCount = 6,
            GridFightSyncCurtaskInfo = new GridFightSyncCurrentTaskInfo(),
            GameLockInfo = new GridFightLockInfo()
        }
    };

    private GridFightGameInfo BuildTeamSection()
    {
        var team = new GridFightGameTeamInfo();
        foreach (var (pos, uniqueId) in UniqueIdByPos.OrderBy(kv => kv.Key))
        {
            if (uniqueId == 0 || !RoleByUniqueId.TryGetValue(uniqueId, out var avatarId)) continue;
            team.GridGameRoleList.Add(new GridGameRoleInfo
            {
                Id = avatarId,
                Pos = pos,
                RoleStar = RoleStarByUniqueId.GetValueOrDefault(uniqueId, 1u),
                UniqueId = uniqueId
            });
        }
        return new GridFightGameInfo { GridTraitGameInfo = team };
    }

    private GridFightGameInfo BuildDraftSection()
    {
        var draft = new GridFightGameItemsInfo();
        foreach (var c in Consumables) draft.GridFightConsumableList.Add(c);
        foreach (var e in Equipments) draft.GridFightEquipmentList.Add(e);
        return new GridFightGameInfo { GridDraftInfo = draft };
    }

    private GridFightGameInfo BuildItemsShopSection() => new()
    {
        GridItemsInfo = new GridFightGameShopInfo
        {
            GLIFNMBMMBL = ShopRefreshLeft,
            DNOIFMMLJDN = { ShopRolePool },
            LDEDGOOKHFL = new FJPONJFLOOH
            {
                EDJPMNLLGGB =
                {
                    new MJJEHCBNOKI { MMKNFIFOPPA = 1, FLICPMGFKOK = 100 },
                    new MJJEHCBNOKI { MMKNFIFOPPA = 2 },
                    new MJJEHCBNOKI { MMKNFIFOPPA = 3 },
                    new MJJEHCBNOKI { MMKNFIFOPPA = 4 },
                    new MJJEHCBNOKI { MMKNFIFOPPA = 5 }
                }
            },
            ShopGoodsList = { ShopGoods }
        }
    };

    private GridFightGameInfo BuildOrbSection() => new()
    {
        GridLevelInfo = new GridFightGameOrbInfo
        {
            GridGameOrbList = { new GridGameOrbInfo { OrbItemId = 199, UniqueId = 7 } }
        }
    };

    private GridFightGameInfo BuildLevelSection()
    {
        var lvl = new GridFightLevelInfo
        {
            DCPKPNLKGMM = Level,
            NDOCIKPLKIF = NDOCIKPLKIF,
            SectionId = SectionId,
            ECCGJDMOGAN = new DDJIOFONKME(),
            BossInfo = new GridFightBossInfo(),
            CMHBDMOJJEN = new IKFEDFBLOOG()
        };
        EnsureSessionPreview();
        foreach (var campId in SessionCampIds)
            lvl.HGAHMIPIBLO.Add(new OPBCCOLPDPC { PMOGHFIGKPO = campId });
        foreach (var affix in SectionAffixIds)
            lvl.IAKFPMOEJLF.Add(new DIBJGAKOCLO { AffixId = affix });
        foreach (var portalBuff in ActivePortalBuffIds)
            lvl.GridFightPortalBuffList.Add(new GridFightGamePortalBuffInfo { PortalBuffId = portalBuff });
        return new GridFightGameInfo { GridShopInfo = lvl };
    }

    private GridFightGameInfo BuildAugmentSection()
    {
        var trait = new GridFightGameTraitInfo();
        trait.ALIDDLBDPDH.Add(new ELEOGABGBKG { DMEKIFJDKFL = 8007, CELFGCJFMPH = { 8007u, 8009u } });
        trait.ALIDDLBDPDH.Add(new ELEOGABGBKG { DMEKIFJDKFL = 11011, CELFGCJFMPH = { 11011u, 11012u } });
        return new GridFightGameInfo { GridAugmentInfo = trait };
    }

    private GridFightGameInfo BuildTeamGameInfoSection() => new()
    {
        GridTeamGameInfo = new LHFDOPGEOML
        {
            ANLGPLOLMOH = 8,
            OGHGLMGJGEM = { ["GP_Avatar_Sparxie_00"] = 2 }
        }
    };
}
