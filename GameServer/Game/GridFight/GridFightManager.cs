using March7thHoney.Data;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

public class GridFightManager(PlayerInstance player) : BasePlayerManager(player)
{
    public GridFightInstance? GridFightInstance { get; set; }
    public uint CurUniqueId { get; set; }

    public GridFightSystemInfo ToProto()
    {
        var staticInfo = new GridFightStaticGameInfo
        {
            DivisionId = 10706,
            GridFightComboWinNum = 1,
            CKFIACKHNAE = 1,
            CALIMAKGGHJ = 3,
            Exp = new JIBAKJGOPJM
            {
                BPJAFBFFPPH = 1500
            },
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

        staticInfo.OJLAODIALLE.DANAGDAPKJE.Add([2011, 2021, 2022, 2031, 2032, 2033, 2041, 2042, 2043, 2044, 2051, 2061, 2062, 2063, 3011, 3021, 3022, 3031, 3032, 3033, 3041, 3042, 3043, 3044, 3051, 3061, 3062, 3063, 4011, 4021, 4022, 4031, 4032, 4041, 4042, 4043, 4051, 4052, 4053, 4061]);
        staticInfo.EJCFDAABLOC.GridFightMonsterInfo.HGAHMIPIBLO.Add([1, 2, 4, 5, 6, 8, 10, 11, 12, 13, 15, 16, 17, 23, 24, 25, 27]);
        staticInfo.EJCFDAABLOC.GridFightRoleInfo.GridFightAvatarList.Add([1001, 1003, 1004, 1005, 1009, 1013, 1014, 1015, 1105, 1108, 1112, 1202, 1203, 1204, 1205, 1208, 1209, 1212, 1213, 1217, 1218, 1220, 1221, 1222, 1223, 1225, 1301, 1302, 1303, 1304, 1305, 1306, 1307, 1308, 1309, 1310, 1313, 1314, 1317, 1401, 1402, 1403, 1404, 1405, 1406, 1407, 1408, 1409, 1410, 1412, 1413, 1414, 1415, 1501, 1502, 1505, 8007, 8009, 11011, 11012, 15061, 15062, 15063]);
        staticInfo.EJCFDAABLOC.GridFightPortalInfo.PELJLONLDNM.Add([101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 127, 129, 134, 135, 138, 147, 1001, 1002, 1003, 1004, 1005, 1007, 1008, 1010, 1014, 1016, 1101, 1102, 1104, 1106, 1107, 1112, 1113, 1114, 1115, 1116, 1118]);
        staticInfo.EJCFDAABLOC.GridFightPortalInfo.GridFightPortalBuffList.Add([101, 105, 106, 107, 108, 109, 110, 112, 113, 115, 120, 121, 123, 124, 127, 129, 138, 1002, 1003, 1004, 1007, 1014, 1016, 1112, 1113, 1116]);
        staticInfo.EJCFDAABLOC.GridFightEquipInfo.GridFightItemList.Add([99990, 99991, 99992, 99998, 99999, 350101, 350102, 350103, 350104, 350105, 350106, 350107, 350201, 350202, 350203, 350204, 350205, 350206, 350207, 350208, 350601, 350602, 350701, 352101, 352502, 35030101, 35030102, 35030103, 35030104, 35030105, 35030106, 35030107, 35030108, 35030202, 35030203, 35030204, 35030205, 35030206, 35030207, 35030208, 35030303, 35030304, 35030305, 35030306, 35030307, 35030308, 35030404, 35030405, 35030406, 35030407, 35030408, 35030505, 35030506, 35030507, 35030508, 35030606, 35030607, 35030608, 35030707, 35030708, 35030808, 35040101, 35051001, 35051006, 35051007, 35051008, 35051010, 35051012, 35052001, 35052002, 35052003, 35052004, 35052008, 35052009, 35100001, 35100002]);
        staticInfo.EJCFDAABLOC.GridFightAugmentInfo.JLCDEDKPEAB.Add([100301, 100401, 100402, 100702, 100901, 101101, 101401, 101601, 101602, 102501, 102901, 103001, 103401, 103701, 110102, 150101, 150201, 150301, 150901, 151101, 200201, 200301, 200502, 200601, 200701, 200801, 201001, 201201, 201302, 201601, 202001, 203201, 203302, 210701, 210801, 211001, 220401, 231101, 250601, 250701, 250801, 251301, 251401, 251601, 252201, 252401, 253101, 253201, 300401, 300701, 301002, 301301, 301401, 301601, 301701, 301901, 302601, 302701, 320101, 320301, 320401, 330101, 330201, 330901, 331101, 332201, 350101, 350301, 350501, 350601, 350701, 350901, 351101, 351401, 351601, 351701, 360201]);
        staticInfo.EJCFDAABLOC.GridFightAugmentInfo.KDFBCMANFMB.Add([100101, 100301, 100401, 100402, 100403, 100501, 100601, 100701, 100702, 100901, 101001, 101101, 101201, 101202, 101301, 101401, 101402, 101501, 101601, 101602, 101701, 101801, 101901, 102001, 102101, 102201, 102501, 102502, 102601, 102801, 102901, 103001, 103201, 103202, 103401, 103402, 103701, 110101, 110102, 130101, 130301, 130401, 130501, 130701, 131001, 131101, 150101, 150201, 150301, 150601, 150701, 150801, 150901, 151101, 200101, 200201, 200301, 200502, 200601, 200602, 200701, 200801, 201001, 201102, 201201, 201301, 201302, 201501, 201601, 201801, 201901, 202001, 202101, 202201, 202301, 202501, 202701, 202901, 203001, 203201, 203302, 210301, 210701, 210801, 211001, 220401, 220501, 220701, 220801, 230201, 230301, 230401, 230501, 230601, 230801, 230901, 231101, 231201, 231301, 231601, 250101, 250301, 250401, 250601, 250701, 250801, 250901, 251001, 251201, 251301, 251401, 251501, 251601, 251801, 252001, 252101, 252201, 252301, 252401, 252501, 252601, 252701, 252801, 252901, 253101, 253201, 260301, 300101, 300401, 300501, 300701, 300801, 300901, 301001, 301002, 301101, 301301, 301401, 301601, 301701, 301801, 301901, 302001, 302201, 302601, 302701, 302901, 303001, 303101, 303401, 310501, 310601, 310701, 311101, 320101, 320201, 320301, 320401, 320801, 320901, 321101, 330101, 330201, 330301, 330401, 330501, 330701, 330801, 330901, 331001, 331101, 331201, 331301, 331401, 331501, 331601, 331901, 332101, 332201, 350101, 350301, 350402, 350403, 350501, 350601, 350701, 350801, 350901, 351001, 351101, 351401, 351501, 351601, 351701, 351801, 360201]);

        var system = new GridFightSystemInfo
        {
            EGLCKGKECAJ = staticInfo,
            FCKKGFFLDFA = new MIGEAHDEBOE
            {
                OJLAODIALLE = new GridFightTalentInfo()
            }
        };
        system.FCKKGFFLDFA.OJLAODIALLE.DANAGDAPKJE.Add([1011, 1021, 1022, 1031, 1041, 1042, 1051, 1061, 1062, 1071, 1072, 1073, 1081]);
        return system;
    }

    public GridFightSyncUpdateResultScNotify BuildSyncUpdateNotify(IEnumerable<GridFightPosInfo>? updatedPosList = null)
    {
        var notify = new GridFightSyncUpdateResultScNotify();

        if (updatedPosList != null && GridFightInstance != null)
        {
            var sync = new GridFightSyncResultData();
            foreach (var posInfo in updatedPosList)
            {
                if (posInfo.Pos == 0 || posInfo.UniqueId == 0)
                    continue;
                if (!GridFightInstance.RoleByUniqueId.TryGetValue(posInfo.UniqueId, out var avatarId))
                    continue;

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
        }
        else
        {
            var augmentSync = new GridFightSyncResultData();
            var augmentItem = new GridFightSyncData
            {
                GMJLJDJDIGM = new JPBCKCDEGOM()
            };
            augmentItem.GMJLJDJDIGM.ALIDDLBDPDH.Add(new ELEOGABGBKG
            {
                DMEKIFJDKFL = 8007,
                CELFGCJFMPH = { 8007, 8009 }
            });
            augmentItem.GMJLJDJDIGM.ALIDDLBDPDH.Add(new ELEOGABGBKG
            {
                DMEKIFJDKFL = 11011,
                CELFGCJFMPH = { 11011, 11012 }
            });
            augmentSync.UpdateDynamicList.Add(augmentItem);
            notify.SyncResultDataList.Add(augmentSync);

            var portalAndRoleSync = new GridFightSyncResultData
            {
                GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpHndkhmefaal
            };
            portalAndRoleSync.UpdateDynamicList.Add(new GridFightSyncData
            {
                PortalServerDataUpdate = new GridFightPortalServerDataUpdate
                {
                    PortalBuffId = 111
                }
            });
            notify.SyncResultDataList.Add(portalAndRoleSync);

            var addRoleSync = new GridFightSyncResultData
            {
                GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpBjdeaahibge
            };
            addRoleSync.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = 3 });
            addRoleSync.UpdateDynamicList.Add(new GridFightSyncData
            {
                AddRoleInfo = new GridGameRoleInfo { Id = 1009, Pos = 14, RoleStar = 1, UniqueId = 3, GridFightValueInitComponent = { ["GP_Avatar_Asta_01"] = 0 } }
            });
            addRoleSync.UpdateDynamicList.Add(new GridFightSyncData
            {
                AddRoleInfo = new GridGameRoleInfo { Id = 1306, Pos = 15, RoleStar = 1, UniqueId = 4, GridFightValueInitComponent = { ["GP_Avatar_Sparkle_01"] = 0 } }
            });
            addRoleSync.UpdateDynamicList.Add(new GridFightSyncData
            {
                AddRoleInfo = new GridGameRoleInfo { Id = 1403, Pos = 16, RoleStar = 1, UniqueId = 5, GridFightValueInitComponent = { ["GP_Avatar_Tribbie_01"] = 0 } }
            });
            addRoleSync.UpdateDynamicList.Add(new GridFightSyncData
            {
                AddRoleInfo = new GridGameRoleInfo { Id = 1502, Pos = 17, RoleStar = 1, UniqueId = 6, GridFightValueInitComponent = { ["GP_Avatar_YaoGuang_01"] = 0 } }
            });
            notify.SyncResultDataList.Add(addRoleSync);

            var finishPendingSync = new GridFightSyncResultData();
            finishPendingSync.UpdateDynamicList.Add(new GridFightSyncData { FinishPendingActionPos = 1 });
            finishPendingSync.UpdateDynamicList.Add(new GridFightSyncData { SyncLockInfo = new GridFightLockInfo() });
            notify.SyncResultDataList.Add(finishPendingSync);

            var shopSync = new GridFightSyncResultData();
            shopSync.UpdateDynamicList.Add(new GridFightSyncData
            {
                ShopSyncInfo = new GridFightShopSyncInfo
                {
                    GLIFNMBMMBL = 2,
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
                    ShopGoodsList =
                    {
                        new GridFightShopGoodsInfo { ShopGoodsPrice = 1, RoleGoodsInfo = new GridFightRoleGoodsInfo { RoleId = 1217, RoleStar = 1 } },
                        new GridFightShopGoodsInfo { ShopGoodsPrice = 1, RoleGoodsInfo = new GridFightRoleGoodsInfo { RoleId = 1314, RoleStar = 1 } },
                        new GridFightShopGoodsInfo { ShopGoodsPrice = 1, RoleGoodsInfo = new GridFightRoleGoodsInfo { RoleId = 1108, RoleStar = 1 } },
                        new GridFightShopGoodsInfo { ShopGoodsPrice = 1, RoleGoodsInfo = new GridFightRoleGoodsInfo { RoleId = 1220, RoleStar = 1 } },
                        new GridFightShopGoodsInfo { ShopGoodsPrice = 1, RoleGoodsInfo = new GridFightRoleGoodsInfo { RoleId = 1406, RoleStar = 1 } }
                    }
                }
            });
            notify.SyncResultDataList.Add(shopSync);

            var lockAndPendingSync = new GridFightSyncResultData();
            lockAndPendingSync.UpdateDynamicList.Add(new GridFightSyncData
            {
                SyncLockInfo = new GridFightLockInfo
                {
                    LockReason = GridFightLockReason.DfofffceffoKjmjdbjmbmc,
                    LockType = GridFightLockType.PjbmhhnlclbEhfhdgpocnh
                }
            });
            lockAndPendingSync.UpdateDynamicList.Add(new GridFightSyncData
            {
                PendingAction = new GridFightPendingAction
                {
                    QueuePosition = 2,
                    RoundBeginAction = new GridFightRoundBeginActionInfo()
                }
            });
            notify.SyncResultDataList.Add(lockAndPendingSync);

            var finishReturnSync = new GridFightSyncResultData();
            finishReturnSync.UpdateDynamicList.Add(new GridFightSyncData { FinishPendingActionPos = 2 });
            finishReturnSync.UpdateDynamicList.Add(new GridFightSyncData { SyncLockInfo = new GridFightLockInfo() });
            notify.SyncResultDataList.Add(finishReturnSync);

            var returnPendingSync = new GridFightSyncResultData();
            returnPendingSync.UpdateDynamicList.Add(new GridFightSyncData
            {
                SyncLockInfo = new GridFightLockInfo
                {
                    LockReason = GridFightLockReason.DfofffceffoKjmjdbjmbmc,
                    LockType = GridFightLockType.PjbmhhnlclbEhfhdgpocnh
                }
            });
            returnPendingSync.UpdateDynamicList.Add(new GridFightSyncData
            {
                PendingAction = new GridFightPendingAction
                {
                    QueuePosition = 8,
                    ReturnPreparationAction = new GridFightReturnPreparationActionInfo()
                }
            });
            notify.SyncResultDataList.Add(returnPendingSync);
        }
        return notify;
    }

    public GridFightSettleNotify BuildSettleNotify()
    {
        var notify = new GridFightSettleNotify
        {
            BHLDAEKNMCD = 111,
            EDKIICIKJKL = 10706,
            OHOPKAAKOGF = 10706,
            EDKJMPACHNJ = new GridFightFinishInfo
            {
                BCHPAOCOHIL = new FCBEHGJBJCN
                {
                    AMNJHJJMPJF = { 35030205u, 35030405u, 35030208u }
                },
                NLILNONCNFC = new JCFJADFEOJN
                {
                    BBDOCJGAEEJ = 10706,
                    BCOLJFHDLLD = 80,
                    BFNPCJOMGFL = 3,
                    DCPKPNLKGMM = 1,
                    IJEIEJLPGGJ = 100,
                    NDOCIKPLKIF = 1600,
                    SectionId = 1
                },
                PGPKPMOIAIL = new CLOEPPBCKGF
                {
                    GridFightAvatarList = { 1006u, 1201u, 1202u, 1223u, 1301u, 15062u, 15063u }
                }
            }
        };

        notify.EDKJMPACHNJ.CEAFFNCKDDD.Add(new GridFightGamePortalBuffInfo { PortalBuffId = 106 });
        notify.EDKJMPACHNJ.IAKFPMOEJLF.Add(new DIBJGAKOCLO { AffixId = 1002 });
        notify.EDKJMPACHNJ.IAKFPMOEJLF.Add(new DIBJGAKOCLO { AffixId = 3005 });
        notify.EDKJMPACHNJ.IAKFPMOEJLF.Add(new DIBJGAKOCLO { AffixId = 4010 });
        notify.EDKJMPACHNJ.GridFightEquipmentList.Add(new GridFightEquipmentInfo
        {
            GridFightEquipmentId = 350205,
            UniqueId = 10
        });

        return notify;
    }

    public GridFightCurrentInfo BuildCurrentInfo()
    {
        var info = GridFightInstance?.ToProto() ?? new GridFightCurrentInfo
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
                    GridFightPortalBuffList = { 1115u, 106u, 147u }
                }
            },
            BCHPAOCOHIL = new FCBEHGJBJCN
            {
                AMNJHJJMPJF = { 35030205u, 35030405u, 35030208u }
            },
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
            },
            RogueCurrentGameInfo =
            {
                new GridFightGameInfo
                {
                    GridBasicInfo = new GridFightGameBasicInfo
                    {
                        ANBBPPHBCJH = 3,
                        FLEJPPKLJIC = 3,
                        HAEOPKELNEO = 10,
                        Gold = 3,
                        GridFightBuyExpCost = 4,
                        GridFightLineupHp = 80,
                        GridFightLineupMaxHp = 100,
                        GridFightMaxAvatarCount = 9,
                        GridFightMaxInterestGold = 5,
                        GridFightOffFieldMaxCount = 6,
                        GridFightSyncCurtaskInfo = new GridFightSyncCurrentTaskInfo(),
                        GameLockInfo = new GridFightLockInfo()
                    }
                },
                new GridFightGameInfo
                {
                    GridTraitGameInfo = new GridFightGameTeamInfo
                    {
                        GridGameRoleList =
                        {
                            new GridGameRoleInfo { Id = 1009, Pos = 14, RoleStar = 1, UniqueId = 3, GridFightValueInitComponent = { ["GP_Avatar_Asta_01"] = 0 } },
                            new GridGameRoleInfo { Id = 1205, Pos = 15, RoleStar = 1, UniqueId = 4, GridFightValueInitComponent = { ["GP_Avatar_Ren_01"] = 0 } },
                            new GridGameRoleInfo { Id = 1321, Pos = 16, RoleStar = 1, UniqueId = 5, GridFightValueInitComponent = { ["GP_Avatar_Constance_01"] = 0 } },
                            new GridGameRoleInfo { Id = 1408, Pos = 17, RoleStar = 1, UniqueId = 6, GridFightValueInitComponent = { ["GP_Avatar_Phainon_01"] = 0 } }
                        }
                    }
                },
                new GridFightGameInfo
                {
                    GridDraftInfo = new GridFightGameItemsInfo
                    {
                        GridFightConsumableList = { new GridFightConsumableInfo { ItemId = 350101, Num = 1 } },
                        GridFightEquipmentList = { new GridFightEquipmentInfo { GridFightEquipmentId = 350205, Source = 1, UniqueId = 10 } }
                    }
                },
                new GridFightGameInfo
                {
                    GridItemsInfo = new GridFightGameShopInfo
                    {
                        GLIFNMBMMBL = 2,
                        DNOIFMMLJDN = { 15063u, 15062u, 1202u, 1301u, 1006u, 1201u, 1223u },
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
                        ShopGoodsList =
                        {
                            new GridFightShopGoodsInfo { ShopGoodsPrice = 1, RoleGoodsInfo = new GridFightRoleGoodsInfo { RoleId = 1321, RoleStar = 1 } },
                            new GridFightShopGoodsInfo { ShopGoodsPrice = 1, RoleGoodsInfo = new GridFightRoleGoodsInfo { RoleId = 1108, RoleStar = 1 } },
                            new GridFightShopGoodsInfo { ShopGoodsPrice = 1, RoleGoodsInfo = new GridFightRoleGoodsInfo { RoleId = 1314, RoleStar = 1 } },
                            new GridFightShopGoodsInfo { ShopGoodsPrice = 1, RoleGoodsInfo = new GridFightRoleGoodsInfo { RoleId = 1406, RoleStar = 1 } },
                            new GridFightShopGoodsInfo { ShopGoodsPrice = 1, RoleGoodsInfo = new GridFightRoleGoodsInfo { RoleId = 1001, RoleStar = 1 } }
                        }
                    }
                },
                new GridFightGameInfo
                {
                    GridLevelInfo = new GridFightGameOrbInfo
                    {
                        GridGameOrbList = { new GridGameOrbInfo { OrbItemId = 199, UniqueId = 7 } }
                    }
                },
                new GridFightGameInfo
                {
                    GridShopInfo = new GridFightLevelInfo
                    {
                        DCPKPNLKGMM = 1,
                        NDOCIKPLKIF = 1600,
                        SectionId = 1,
                        ECCGJDMOGAN = new DDJIOFONKME(),
                        BossInfo = new GridFightBossInfo(),
                        CMHBDMOJJEN = new IKFEDFBLOOG(),
                        HGAHMIPIBLO =
                        {
                            new OPBCCOLPDPC { PMOGHFIGKPO = 17 },
                            new OPBCCOLPDPC { PMOGHFIGKPO = 8 },
                            new OPBCCOLPDPC { PMOGHFIGKPO = 24 }
                        },
                        IAKFPMOEJLF =
                        {
                            new DIBJGAKOCLO { AffixId = 1002 },
                            new DIBJGAKOCLO { AffixId = 3005 },
                            new DIBJGAKOCLO { AffixId = 4010 }
                        },
                        GridFightPortalBuffList = { new GridFightGamePortalBuffInfo { PortalBuffId = 106 } }
                    }
                },
                new GridFightGameInfo { GridOrbInfo = new GridFightGameAugmentSync() },
                new GridFightGameInfo
                {
                    GridAugmentInfo = new GridFightGameTraitInfo
                    {
                        ALIDDLBDPDH =
                        {
                            new ELEOGABGBKG { DMEKIFJDKFL = 8007, CELFGCJFMPH = { 8007u, 8009u } },
                            new ELEOGABGBKG { DMEKIFJDKFL = 11011, CELFGCJFMPH = { 11011u, 11012u } }
                        }
                    }
                },
                new GridFightGameInfo
                {
                    GridTeamGameInfo = new LHFDOPGEOML
                    {
                        ANLGPLOLMOH = 8,
                        OGHGLMGJGEM = { ["GP_Avatar_Sparxie_00"] = 2 }
                    }
                }
            }
        };

        return info;
    }

    public (Retcode, GridFightInstance?) StartGamePlay(uint season, uint divisionId, bool isOverLock)
    {
        if (GridFightInstance != null)
            return (Retcode.RetSucc, GridFightInstance);

        GridFightInstance = new GridFightInstance(Player, season == 0 ? 1u : season, divisionId == 0 ? 10939u : divisionId,
            isOverLock, ++CurUniqueId);
        return (Retcode.RetSucc, GridFightInstance);
    }
}
