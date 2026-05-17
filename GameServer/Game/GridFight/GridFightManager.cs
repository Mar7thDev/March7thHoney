using March7thHoney.Data;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.GridFight.Sync;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

public class GridFightManager(PlayerInstance player) : BasePlayerManager(player)
{
    public GridFightInstance? GridFightInstance { get; set; }
    public uint CurUniqueId { get; set; }

    public GridFightSystemInfo ToProto()
    {
        var divisionId = GameData.GridFightDivisionInfoData.Count > 0
            ? GameData.GridFightDivisionInfoData.Keys.Max()
            : 10939u;

        var staticInfo = new GridFightStaticGameInfo
        {
            DivisionId = divisionId,
            CKFIACKHNAE = 1,
            CALIMAKGGHJ = 3,
            Exp = new JIBAKJGOPJM(),
            OJLAODIALLE = new GridFightTalentInfo(),
            EJCFDAABLOC = new GridFightHandBookInfo
            {
                GridFightMonsterInfo = new GridFightHandBookMonsterInfo(),
                GridFightRoleInfo = new GridFightHandBookRoleInfo(),
                GridFightPortalInfo = new GridFightHandBookPortalInfo(),
                GridFightEquipInfo = new GridFightHandBookEquipInfo(),
                GridFightAugmentInfo = new GridFightHandBookAugmentInfo()
            }
        };

        if (GameData.GridFightSeasonTalentData.Count > 0)
            staticInfo.OJLAODIALLE.DANAGDAPKJE.Add(GameData.GridFightSeasonTalentData.Keys.OrderBy(x => x));
        else
            staticInfo.OJLAODIALLE.DANAGDAPKJE.Add([2011, 2021, 2022, 2031, 2032, 2033, 2041, 2042, 2043, 2044, 2051, 2061, 2062, 2063, 3011, 3021, 3022, 3031, 3032, 3033, 3041, 3042, 3043, 3044, 3051, 3061, 3062, 3063, 4011, 4021, 4022, 4031, 4032, 4041, 4042, 4043, 4051, 4052, 4053, 4061]);
        staticInfo.EJCFDAABLOC.GridFightMonsterInfo.HGAHMIPIBLO.Add([1, 2, 4, 5, 6, 8, 10, 11, 12, 13, 15, 16, 17, 23, 24, 25, 27]);
        staticInfo.EJCFDAABLOC.GridFightRoleInfo.GridFightAvatarList.Add([1001, 1003, 1004, 1005, 1009, 1013, 1014, 1015, 1105, 1108, 1112, 1202, 1203, 1204, 1205, 1208, 1209, 1212, 1213, 1217, 1218, 1220, 1221, 1222, 1223, 1225, 1301, 1302, 1303, 1304, 1305, 1306, 1307, 1308, 1309, 1310, 1313, 1314, 1317, 1401, 1402, 1403, 1404, 1405, 1406, 1407, 1408, 1409, 1410, 1412, 1413, 1414, 1415, 1501, 1502, 1505, 8007, 8009, 11011, 11012, 15061, 15062, 15063]);
        staticInfo.EJCFDAABLOC.GridFightPortalInfo.PELJLONLDNM.Add([101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 127, 129, 134, 135, 138, 147, 1001, 1002, 1003, 1004, 1005, 1007, 1008, 1010, 1014, 1016, 1101, 1102, 1104, 1106, 1107, 1112, 1113, 1114, 1115, 1116, 1118]);
        staticInfo.EJCFDAABLOC.GridFightPortalInfo.GridFightPortalBuffList.Add([101, 105, 106, 107, 108, 109, 110, 112, 113, 115, 120, 121, 123, 124, 127, 129, 138, 1002, 1003, 1004, 1007, 1014, 1016, 1112, 1113, 1116]);
        staticInfo.EJCFDAABLOC.GridFightEquipInfo.GridFightItemList.Add([99990, 99991, 99992, 99998, 99999, 350101, 350102, 350103, 350104, 350105, 350106, 350107, 350201, 350202, 350203, 350204, 350205, 350206, 350207, 350208, 350601, 350602, 350701, 352101, 352502, 35030101, 35030102, 35030103, 35030104, 35030105, 35030106, 35030107, 35030108, 35030202, 35030203, 35030204, 35030205, 35030206, 35030207, 35030208, 35030303, 35030304, 35030305, 35030306, 35030307, 35030308, 35030404, 35030405, 35030406, 35030407, 35030408, 35030505, 35030506, 35030507, 35030508, 35030606, 35030607, 35030608, 35030707, 35030708, 35030808, 35040101, 35051001, 35051006, 35051007, 35051008, 35051010, 35051012, 35052001, 35052002, 35052003, 35052004, 35052008, 35052009, 35100001, 35100002]);
        staticInfo.EJCFDAABLOC.GridFightAugmentInfo.JLCDEDKPEAB.Add([100301, 100401, 100402, 100702, 100901, 101101, 101401, 101601, 101602, 102501, 102901, 103001, 103401, 103701, 110102, 150101, 150201, 150301, 150901, 151101, 200201, 200301, 200502, 200601, 200701, 200801, 201001, 201201, 201302, 201601, 202001, 203201, 203302, 210701, 210801, 211001, 220401, 231101, 250601, 250701, 250801, 251301, 251401, 251601, 252201, 252401, 253101, 253201, 300401, 300701, 301002, 301301, 301401, 301601, 301701, 301901, 302601, 302701, 320101, 320301, 320401, 330101, 330201, 330901, 331101, 332201, 350101, 350301, 350501, 350601, 350701, 350901, 351101, 351401, 351601, 351701, 360201]);
        staticInfo.EJCFDAABLOC.GridFightAugmentInfo.KDFBCMANFMB.Add([100101, 100301, 100401, 100402, 100403, 100501, 100601, 100701, 100702, 100901, 101101, 101102, 101201, 101301, 101401, 101402, 101501, 101601, 101602, 101701, 101801, 101901, 102001, 102101, 102201, 102501, 102502, 102601, 102801, 102901, 103001, 103201, 103202, 103401, 103402, 103701, 110101, 110102, 130101, 130301, 130401, 130501, 130701, 131001, 131101, 150101, 150201, 150301, 150601, 150701, 150801, 150901, 151101, 200101, 200201, 200301, 200502, 200601, 200602, 200701, 200801, 201001, 201102, 201201, 201301, 201302, 201501, 201601, 201801, 201901, 202001, 202101, 202201, 202301, 202501, 202701, 202901, 203001, 203201, 203302, 210301, 210701, 210801, 211001, 220401, 220501, 220701, 220801, 230201, 230301, 230401, 230501, 230601, 230801, 230901, 231101, 231201, 231301, 231601, 250101, 250301, 250401, 250601, 250701, 250801, 250901, 251001, 251201, 251301, 251401, 251501, 251601, 251801, 252001, 252101, 252201, 252301, 252401, 252501, 252601, 252701, 252801, 252901, 253101, 253201, 260301, 300101, 300401, 300501, 300701, 300801, 300901, 301001, 301002, 301101, 301301, 301401, 301601, 301701, 301801, 301901, 302001, 302201, 302601, 302701, 302901, 303001, 303101, 303401, 310501, 310601, 310701, 311101, 320101, 320201, 320301, 320401, 320801, 320901, 321101, 330101, 330201, 330301, 330401, 330501, 330701, 330801, 330901, 331001, 331101, 331201, 331301, 331401, 331501, 331601, 331901, 332101, 332201, 350101, 350301, 350402, 350403, 350501, 350601, 350701, 350801, 350901, 351001, 351101, 351401, 351501, 351601, 351701, 351801, 360201]);

        var system = new GridFightSystemInfo
        {
            EGLCKGKECAJ = staticInfo,
            FCKKGFFLDFA = new MIGEAHDEBOE { OJLAODIALLE = new GridFightTalentInfo() }
        };
        if (GameData.GridFightTalentData.Count > 0)
            system.FCKKGFFLDFA.OJLAODIALLE.DANAGDAPKJE.Add(GameData.GridFightTalentData.Keys.OrderBy(x => x));
        else
            system.FCKKGFFLDFA.OJLAODIALLE.DANAGDAPKJE.Add([1011, 1021, 1022, 1031, 1041, 1042, 1051, 1061, 1062, 1071, 1072, 1073, 1081]);
        return system;
    }

    public GridFightSyncUpdateResultScNotify BuildSyncUpdateNotify(
        IEnumerable<GridFightPosInfo>? updatedPosList = null,
        int kind = GridFightSyncKind.Bootstrap,
        object? extra = null)
    {
        if (updatedPosList != null) return BuildPosUpdateSync(updatedPosList);
        return kind switch
        {
            GridFightSyncKind.Bootstrap      => BuildBootstrapSync(),
            GridFightSyncKind.PendingAdvance => BuildPendingAdvanceSync(extra),
            GridFightSyncKind.SelectEquip    => BuildSelectEquipSync(extra),
            GridFightSyncKind.BuyGoods       => BuildBuyGoodsSync(extra),
            GridFightSyncKind.RefreshShop    => BuildRefreshShopSync(),
            GridFightSyncKind.RecycleRole    => BuildRecycleRoleSync(extra),
            GridFightSyncKind.BuyExp         => BuildBuyExpSync(),
            GridFightSyncKind.PostBattle     => BuildPostBattleSync(),
            GridFightSyncKind.PreSettle      => BuildPreSettleSync(),
            GridFightSyncKind.NoOp           => new GridFightSyncUpdateResultScNotify(),
            _                                => new GridFightSyncUpdateResultScNotify()
        };
    }

    private GridFightSyncUpdateResultScNotify BuildPosUpdateSync(IEnumerable<GridFightPosInfo> updatedPosList)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        if (GridFightInstance == null) return notify;
        var sync = new GridFightSyncResultData();
        foreach (var posInfo in updatedPosList)
        {
            if (posInfo.Pos == 0 || posInfo.UniqueId == 0) continue;
            if (!GridFightInstance.RoleByUniqueId.TryGetValue(posInfo.UniqueId, out var avatarId)) continue;
            sync.UpdateDynamicList.Add(new GridFightSyncData
            {
                UpdateRoleInfo = new GridGameRoleInfo
                {
                    Id = avatarId,
                    Pos = posInfo.Pos,
                    UniqueId = posInfo.UniqueId
                }
            });
        }
        notify.SyncResultDataList.Add(sync);
        return notify;
    }

    private GridFightSyncUpdateResultScNotify BuildPendingAdvanceSync(object? extra)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        var inst = GridFightInstance;
        var (oldPos, newPos) = extra is ValueTuple<uint, uint> t ? t : (0u, 0u);

        // 仅当队列真的推进时，才发 FinishPendingActionPos（抓包：reroll 不推进队列时不发）
        if (oldPos > 0 && newPos > oldPos)
        {
            var finishSync = new GridFightSyncResultData();
            finishSync.UpdateDynamicList.Add(new GridFightSyncData { FinishPendingActionPos = oldPos });
            finishSync.UpdateDynamicList.Add(new GridFightSyncData { SyncLockInfo = new GridFightLockInfo() });
            notify.SyncResultDataList.Add(finishSync);
        }

        if (inst?.PendingAction != null)
        {
            var pendingSync = new GridFightSyncResultData();
            pendingSync.UpdateDynamicList.Add(new GridFightSyncData
            {
                SyncLockInfo = new GridFightLockInfo
                {
                    LockReason = GridFightLockReason.DfofffceffoKjmjdbjmbmc,
                    LockType = GridFightLockType.PjbmhhnlclbEhfhdgpocnh
                }
            });
            pendingSync.UpdateDynamicList.Add(new GridFightSyncData { PendingAction = inst.PendingAction });
            notify.SyncResultDataList.Add(pendingSync);
        }
        return notify;
    }

    private GridFightSyncUpdateResultScNotify BuildSelectEquipSync(object? extra)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        var sync = new GridFightSyncResultData();
        if (extra is ValueTuple<uint, uint> ids)
        {
            var (equipId, equipUniqueId) = ids;
            if (equipId > 0)
            {
                sync.UpdateDynamicList.Add(new GridFightSyncData
                {
                    GMJLJDJDIGM = new JPBCKCDEGOM()
                });
                _ = equipUniqueId;
            }
        }
        if (GridFightInstance != null)
        {
            sync.UpdateDynamicList.Add(new GridFightSyncData { FinishPendingActionPos = GridFightInstance.QueuePosition });
        }
        notify.SyncResultDataList.Add(sync);
        return notify;
    }

    private GridFightSyncUpdateResultScNotify BuildBuyGoodsSync(object? extra)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        var sync = new GridFightSyncResultData();
        if (extra is ValueTuple<uint, uint, uint, int> info)
        {
            var (roleId, roleUniqueId, pos, goldDelta) = info;
            sync.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = (uint)Math.Max(0, GridFightInstance?.Gold ?? 0) });
            if (roleUniqueId > 0)
            {
                sync.UpdateDynamicList.Add(new GridFightSyncData
                {
                    AddRoleInfo = new GridGameRoleInfo
                    {
                        Id = roleId,
                        Pos = pos,
                        RoleStar = 1,
                        UniqueId = roleUniqueId
                    }
                });
            }
            _ = goldDelta;
        }
        notify.SyncResultDataList.Add(sync);
        return notify;
    }

    private GridFightSyncUpdateResultScNotify BuildRefreshShopSync()
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        var inst = GridFightInstance;
        if (inst == null) return notify;
        var sync = new GridFightSyncResultData();
        sync.UpdateDynamicList.Add(new GridFightSyncData
        {
            ShopSyncInfo = new GridFightShopSyncInfo
            {
                GLIFNMBMMBL = inst.ShopRefreshLeft,
                LDEDGOOKHFL = new FJPONJFLOOH
                {
                    EDJPMNLLGGB =
                    {
                        new MJJEHCBNOKI { MMKNFIFOPPA = 1, FLICPMGFKOK = 65 },
                        new MJJEHCBNOKI { MMKNFIFOPPA = 2, FLICPMGFKOK = 25 },
                        new MJJEHCBNOKI { MMKNFIFOPPA = 3, FLICPMGFKOK = 10 },
                        new MJJEHCBNOKI { MMKNFIFOPPA = 4 },
                        new MJJEHCBNOKI { MMKNFIFOPPA = 5 }
                    }
                },
                ShopGoodsList = { inst.ShopGoods }
            }
        });
        notify.SyncResultDataList.Add(sync);
        return notify;
    }

    private GridFightSyncUpdateResultScNotify BuildRecycleRoleSync(object? extra)
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        var sync = new GridFightSyncResultData();
        if (extra is ValueTuple<uint, int> info)
        {
            var (removedUniqueId, refund) = info;
            sync.UpdateDynamicList.Add(new GridFightSyncData { RemoveRoleUniqueId = removedUniqueId });
            sync.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = (uint)Math.Max(0, GridFightInstance?.Gold ?? 0) });
            _ = refund;
        }
        notify.SyncResultDataList.Add(sync);
        return notify;
    }

    private GridFightSyncUpdateResultScNotify BuildBuyExpSync()
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        var sync = new GridFightSyncResultData();
        sync.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = (uint)Math.Max(0, GridFightInstance?.Gold ?? 0) });
        notify.SyncResultDataList.Add(sync);
        return notify;
    }

    private GridFightSyncUpdateResultScNotify BuildPostBattleSync()
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        var inst = GridFightInstance;
        if (inst == null) return notify;

        var levelSync = new GridFightSyncResultData();
        var levelInfo = new GridFightLevelSyncInfo
        {
            DCPKPNLKGMM = inst.Level,
            SectionId = inst.SectionId + 1,
            GridFightLayerInfo = new GridFightLayerInfo
            {
                RouteInfo = new GridFightRouteInfo
                {
                    EliteBranchId = 1,
                    FightCampId = inst.CampId
                }
            }
        };
        var encounter = new GridFightEncounterInfo { LFKBMDHKPFI = 90304 };
        var wave = new GridEncounterMonsterWave { IGMMPDDCJIN = 1 };
        foreach (var monsterId in new uint[] { 800101020u, 800105007u, 800105007u, 800105007u, 800104004u })
            wave.PPOEDDFFEKK.Add(new PJLBDMPEKFP { MonsterId = monsterId, RoleStar = 1 });
        encounter.MonsterWaveList.Add(wave);
        levelInfo.GridFightLayerInfo.RouteInfo.RouteEncounterList.Add(encounter);
        levelSync.UpdateDynamicList.Add(new GridFightSyncData { LevelSyncInfo = levelInfo });
        notify.SyncResultDataList.Add(levelSync);

        var lockSync = new GridFightSyncResultData();
        lockSync.UpdateDynamicList.Add(new GridFightSyncData
        {
            SyncLockInfo = new GridFightLockInfo
            {
                LockReason = GridFightLockReason.DfofffceffoKjmjdbjmbmc,
                LockType = GridFightLockType.PjbmhhnlclbEhfhdgpocnh
            }
        });
        lockSync.UpdateDynamicList.Add(new GridFightSyncData
        {
            PendingAction = inst.PendingAction ?? new GridFightPendingAction
            {
                QueuePosition = inst.QueuePosition,
                RoundBeginAction = new GridFightRoundBeginActionInfo()
            }
        });
        notify.SyncResultDataList.Add(lockSync);

        return notify;
    }

    private GridFightSyncUpdateResultScNotify BuildPreSettleSync()
    {
        var notify = new GridFightSyncUpdateResultScNotify();
        var inst = GridFightInstance;
        if (inst == null) return notify;

        var unlock = new GridFightSyncResultData();
        unlock.UpdateDynamicList.Add(new GridFightSyncData { SyncLockInfo = new GridFightLockInfo() });
        notify.SyncResultDataList.Add(unlock);

        var stt = new GridFightSyncResultData
        {
            GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpJalhdinecfe
        };
        stt.SyncEffectParamList.Add(1u);
        stt.SyncEffectParamList.Add(1u);
        if (inst.LastBattleDamageStt != null)
            stt.UpdateDynamicList.Add(new GridFightSyncData { GridFightDamageSttInfo = inst.LastBattleDamageStt.Clone() });
        stt.UpdateDynamicList.Add(new GridFightSyncData
        {
            GridFightLineupHp = new GridFightLineupHpSyncInfo
            {
                GridFightLineupHp = inst.LineupHp,
                GridFightLineupMaxHp = inst.LineupMaxHp
            }
        });
        stt.UpdateDynamicList.Add(new GridFightSyncData
        {
            SectionRecordSyncInfo = new GridFightSectionRecordInfo
            {
                DCPKPNLKGMM = inst.Level,
                SectionId = inst.SectionId,
                CampRecordInfo = new GridFightSectionCampRecordInfo { PMOGHFIGKPO = inst.CampId }
            }
        });
        notify.SyncResultDataList.Add(stt);

        var rewards = new GridFightSyncResultData
        {
            GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpJalhdinecfe
        };
        rewards.SyncEffectParamList.Add(1u);
        rewards.SyncEffectParamList.Add(1u);
        rewards.UpdateDynamicList.Add(new GridFightSyncData
        {
            PlayerLevel = new GridFightPlayerLevelSyncInfo
            {
                Level = inst.PlayerLevel,
                Exp = inst.PlayerExp,
                MaxLevel = inst.PlayerMaxLevel
            }
        });
        if (inst.Gold > inst.PreBattleGold)
        {
            rewards.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = inst.PreBattleGold + 3u });
            rewards.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = inst.Gold });
        }
        foreach (var (itemId, uniqueId) in inst.LastRewardedOrbs)
        {
            rewards.UpdateDynamicList.Add(new GridFightSyncData
            {
                OrbSyncInfo = new GridFightOrbSyncInfo { OrbItemId = itemId, UniqueId = uniqueId }
            });
        }
        notify.SyncResultDataList.Add(rewards);

        return notify;
    }

    private GridFightSyncUpdateResultScNotify BuildBootstrapSync()
    {
        return GridFightBootstrapSyncBuilder.Build();
    }

    public GridFightSettleNotify BuildSettleNotify()
    {
        var inst = GridFightInstance;
        var notify = new GridFightSettleNotify
        {
            BHLDAEKNMCD = inst?.KeepWinCnt ?? 0,
            EDKIICIKJKL = 10706,
            OHOPKAAKOGF = 10706,
            EDKJMPACHNJ = new GridFightFinishInfo
            {
                BCHPAOCOHIL = new FCBEHGJBJCN
                {
                    AMNJHJJMPJF = { inst?.CurrentEquipDraft ?? new List<uint> { 35030205u, 35030405u, 35030208u } }
                },
                NLILNONCNFC = new JCFJADFEOJN
                {
                    BBDOCJGAEEJ = inst?.DivisionId ?? 10706,
                    BCOLJFHDLLD = inst?.LineupHp ?? 80,
                    BFNPCJOMGFL = (uint)(inst?.BattlesFinished ?? 3),
                    DCPKPNLKGMM = inst?.Level ?? 1,
                    IJEIEJLPGGJ = inst?.LineupMaxHp ?? 100,
                    NDOCIKPLKIF = inst?.NDOCIKPLKIF ?? 1600,
                    SectionId = inst?.SectionId ?? 1
                },
                PGPKPMOIAIL = new CLOEPPBCKGF
                {
                    GridFightAvatarList = { 1006u, 1201u, 1202u, 1223u, 1301u, 15062u, 15063u }
                }
            }
        };

        foreach (var portalBuffId in inst?.ActivePortalBuffIds ?? new List<uint> { 106 })
            notify.EDKJMPACHNJ.CEAFFNCKDDD.Add(new GridFightGamePortalBuffInfo { PortalBuffId = portalBuffId });
        foreach (var affixId in inst?.SectionAffixIds ?? new List<uint> { 1002, 3005, 4010 })
            notify.EDKJMPACHNJ.IAKFPMOEJLF.Add(new DIBJGAKOCLO { AffixId = affixId });
        foreach (var equip in inst?.Equipments ?? new List<GridFightEquipmentInfo>())
            notify.EDKJMPACHNJ.GridFightEquipmentList.Add(equip);
        if (notify.EDKJMPACHNJ.GridFightEquipmentList.Count == 0)
            notify.EDKJMPACHNJ.GridFightEquipmentList.Add(new GridFightEquipmentInfo
            {
                GridFightEquipmentId = 350205,
                UniqueId = 10
            });

        return notify;
    }

    public GridFightCurrentInfo BuildCurrentInfo()
    {
        if (GridFightInstance != null) return GridFightInstance.ToProto();

        var stub = new GridFightInstance(Player, 1u, 10706u, false, CurUniqueId);
        return new GridFightCurrentInfo
        {
            Season = 1,
            DivisionId = 10706,
            UniqueId = CurUniqueId,
            PendingAction = new GridFightPendingAction
            {
                QueuePosition = 1,
                PortalBuffAction = new GridFightPortalBuffActionInfo
                {
                    FCHPJKAIBHB = 1,
                    GridFightPortalBuffList = { stub.RollPortalBuffs() }
                }
            }
        };
    }

    public (Retcode, GridFightInstance?) StartGamePlay(uint season, uint divisionId, bool isOverLock)
    {
        if (GridFightInstance != null)
            return (Retcode.RetSucc, GridFightInstance);

        GridFightInstance = new GridFightInstance(Player, season == 0 ? 1u : season,
            divisionId == 0 ? 10939u : divisionId, isOverLock, ++CurUniqueId);
        return (Retcode.RetSucc, GridFightInstance);
    }
}
