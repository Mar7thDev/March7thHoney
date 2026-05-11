using March7thHoney.Data;
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
    public List<GridFightEquipmentInfo> Equipments { get; } = new()
    {
        new GridFightEquipmentInfo { GridFightEquipmentId = 350205, Source = 1, UniqueId = 10 }
    };
    public List<GridFightConsumableInfo> Consumables { get; } = new()
    {
        new GridFightConsumableInfo { ItemId = 350101, Num = 1 }
    };

    // 区段固定数据（增益、关卡）
    public List<uint> SectionAffixIds { get; } = new() { 1002, 3005, 4010 };
    public List<uint> ActivePortalBuffIds { get; } = new() { 106 };
    public List<uint> AvailablePortalBuffRerolls { get; } = new() { 105, 1014, 132 };

    // 初始 4 名 bench 角色（PortalBuff 确认后实例化到 pos 15-18）
    public static readonly (uint AvatarId, uint Pos, uint UniqueId, string Component)[] InitialBenchRoles =
    {
        (1009, 15, 5, "GP_Avatar_Asta_01"),
        (1306, 16, 6, "GP_Avatar_Sparkle_01"),
        (1403, 17, 7, "GP_Avatar_Tribbie_01"),
        (1502, 18, 8, "GP_Avatar_YaoGuang_01")
    };

    public void EnsureDefaultRoles()
    {
        // 抓包确认：StartGamePlay 后初始团队为空，角色在 PortalBuff 确认时通过 sync 实例化
        if (ShopGoods.Count == 0) RotateShop();
    }

    // 在 PortalBuffAction 确认后调用，把 4 名 bench 角色加入实例状态
    public void MaterializeInitialBenchTeam()
    {
        if (RoleByUniqueId.Count > 0) return;
        foreach (var (avatarId, pos, uniqueId, _) in InitialBenchRoles)
        {
            RoleByUniqueId[uniqueId] = avatarId;
            RoleStarByUniqueId[uniqueId] = 1;
            UniqueIdByPos[pos] = uniqueId;
        }
    }

    public uint AllocRoleUniqueId() => ++NextRoleUniqueId;
    public uint AllocEquipUniqueId() => ++NextEquipUniqueId;

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
        if (goods.RoleGoodsInfo == null) return (false, 0, 0, 0);
        var price = goods.ShopGoodsPrice;
        if (Gold < price) Gold = price; // 私服宽松：金币不足时仍发买单
        Gold -= price;
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
        if (ShopRefreshLeft == 0) ShopRefreshLeft = 1; // 私服宽松
        ShopRefreshLeft--;
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
            Gold += 5;
        }
        else
        {
            LineupHp = LineupHp >= 20 ? LineupHp - 20 : 0;
        }
        ShopRefreshLeft = 2;
        RotateShop();
        RefreshEquipDraft();
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
        return new GridFightEndBattleStageNotify
        {
            BCOLJFHDLLD = win ? BattleMaxHp - 20u : 0u,
            DCPKPNLKGMM = Level,
            NDOCIKPLKIF = NDOCIKPLKIF,
            SectionId = SectionId,
            IJEIEJLPGGJ = BattleMaxHp,
            KDOINLGKMBI = BattleMaxHp,
            EJEIBPEKHLD = Level,
            PNOJLNNHBIH = 1,
            HPOACJCPJHN = win
        };
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
        var win = req.EndStatus == BattleEndStatus.BattleEndWin;
        OnBattleResolved(win);
        AdvanceQueue(7);
        PendingAction = new GridFightPendingAction
        {
            QueuePosition = QueuePosition,
            RoundBeginAction = new GridFightRoundBeginActionInfo()
        };

        await Player.SendPacket(new PacketGridFightEndBattleStageNotify(this, win));
        await Player.SendPacket(new PacketGridFightSyncKeepWinCntNotify(KeepWinCnt));
        await Player.SendPacket(new PacketGridFightSeasonHandBookNotify(BuildHandbookNotifyForBattle()));
        await Player.SendPacket(new PacketGridFightHandlePendingActionScRsp(QueuePosition));
        await Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(Player, GridFightSyncKind.PostBattle));
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
                    GridFightPortalBuffList = { 1115u, 106u, 147u }
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
        lvl.HGAHMIPIBLO.Add(new OPBCCOLPDPC { PMOGHFIGKPO = 17 });
        lvl.HGAHMIPIBLO.Add(new OPBCCOLPDPC { PMOGHFIGKPO = 8 });
        lvl.HGAHMIPIBLO.Add(new OPBCCOLPDPC { PMOGHFIGKPO = 24 });
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
