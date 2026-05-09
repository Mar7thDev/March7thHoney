using March7thHoney.GameServer.Game.GridFight.Sync;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.GameServer.Server;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight.PendingAction;

public static class GridFightPendingActionProcessor
{
    public static async System.Threading.Tasks.Task Handle(Connection connection, GridFightInstance inst, GridFightSelectRecommendEquipCsReq req)
    {
        var ackPos = req.QueuePosition;
        if (req.PortalBuffRerollAction != null)
        {
            await HandlePortalBuffReroll(connection, inst, ackPos);
            return;
        }
        if (req.PortalBuffAction != null)
        {
            await HandlePortalBuffSelect(connection, inst, ackPos, req.PortalBuffAction.SelectPortalBuffId);
            return;
        }
        if (req.RoundBeginAction != null)
        {
            await HandleRoundBegin(connection, inst, ackPos);
            return;
        }
        if (req.ReturnPreparationAction != null)
        {
            await HandleReturnPreparation(connection, inst, ackPos);
            return;
        }

        inst.QueuePosition = ackPos + 1;
        inst.PendingAction = new GridFightPendingAction
        {
            QueuePosition = inst.QueuePosition,
            RoundBeginAction = new GridFightRoundBeginActionInfo()
        };
        await connection.SendPacket(new PacketGridFightHandlePendingActionScRsp(ackPos));
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!, GridFightSyncKind.PendingAdvance, (ackPos, inst.QueuePosition)));
    }

    private static async System.Threading.Tasks.Task HandlePortalBuffReroll(Connection connection, GridFightInstance inst, uint ackPos)
    {
        var newBuffList = new List<uint> { 105u, 1014u, 132u };
        inst.QueuePosition = ackPos;
        inst.PendingAction = new GridFightPendingAction
        {
            QueuePosition = ackPos,
            PortalBuffAction = new GridFightPortalBuffActionInfo { FCHPJKAIBHB = 1, GridFightPortalBuffList = { newBuffList } }
        };

        var notify = new GridFightSeasonHandBookNotify { HandbookGridFightPortalInfo = new GridFightHandBookPortalInfo() };
        notify.HandbookGridFightPortalInfo.PELJLONLDNM.Add(132u);
        await connection.SendPacket(new PacketGridFightSeasonHandBookNotify(notify));
        await connection.SendPacket(new PacketGridFightHandlePendingActionScRsp(ackPos));

        var sync = new GridFightSyncUpdateResultScNotify();
        var pendingSection = new GridFightSyncResultData();
        pendingSection.UpdateDynamicList.Add(new GridFightSyncData { PendingAction = inst.PendingAction });
        sync.SyncResultDataList.Add(pendingSection);
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(sync));
    }

    private static async System.Threading.Tasks.Task HandlePortalBuffSelect(Connection connection, GridFightInstance inst, uint ackPos, uint buffId)
    {
        if (buffId > 0 && !inst.ActivePortalBuffIds.Contains(buffId)) inst.ActivePortalBuffIds.Add(buffId);
        inst.MaterializeInitialBenchTeam();
        inst.ConfigureNextBattle(90303, new uint[] { 300101007, 800102021, 300101007 });

        var nextPos = ackPos + 1;
        inst.QueuePosition = nextPos;
        inst.PendingAction = new GridFightPendingAction
        {
            QueuePosition = nextPos,
            RoundBeginAction = new GridFightRoundBeginActionInfo()
        };

        var handbook = new GridFightSeasonHandBookNotify { HandbookGridFightPortalInfo = new GridFightHandBookPortalInfo() };
        handbook.HandbookGridFightPortalInfo.GridFightPortalBuffList.Add(buffId);
        await connection.SendPacket(new PacketGridFightSeasonHandBookNotify(handbook));
        await connection.SendPacket(new PacketGridFightHandlePendingActionScRsp(ackPos));

        var sync = new GridFightSyncUpdateResultScNotify();
        var sec0 = new GridFightSyncResultData { GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpPnejelgglej };
        var equipUid = inst.AllocEquipUniqueId();
        sec0.SyncEffectParamList.Add(buffId);
        sec0.UpdateDynamicList.Add(new GridFightSyncData
        {
            AddGameItemInfo = new GridFightGameItemSyncInfo
            {
                GridFightEquipmentList =
                {
                    new GridFightEquipmentInfo { GridFightEquipmentId = 350602, Source = 1, UniqueId = equipUid }
                }
            }
        });
        sync.SyncResultDataList.Add(sec0);

        var sec1 = new GridFightSyncResultData { GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpPnejelgglej };
        var forgeUid = inst.AllocEquipUniqueId();
        sec1.SyncEffectParamList.Add(buffId);
        sec1.UpdateDynamicList.Add(new GridFightSyncData
        {
            AddForgeInfo = new GridGameForgeItemInfo
            {
                ForgeItemId = 99999,
                Pos = 14,
                UniqueId = forgeUid,
                ForgeGoodsList =
                {
                    new GridFightForgeGoodsInfo { EquipmentGoodsInfo = new GridFightForgeEquipmentInfo { GridFightEquipmentId = 350204 } },
                    new GridFightForgeGoodsInfo { EquipmentGoodsInfo = new GridFightForgeEquipmentInfo { GridFightEquipmentId = 350205 } },
                    new GridFightForgeGoodsInfo { EquipmentGoodsInfo = new GridFightForgeEquipmentInfo { GridFightEquipmentId = 350208 } },
                    new GridFightForgeGoodsInfo { EquipmentGoodsInfo = new GridFightForgeEquipmentInfo { GridFightEquipmentId = 350207 } }
                }
            }
        });
        sync.SyncResultDataList.Add(sec1);

        var sec2 = new GridFightSyncResultData { GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpHndkhmefaal };
        sec2.UpdateDynamicList.Add(new GridFightSyncData { PortalServerDataUpdate = new GridFightPortalServerDataUpdate { PortalBuffId = buffId } });
        sync.SyncResultDataList.Add(sec2);

        var sec3 = new GridFightSyncResultData { GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpBjdeaahibge };
        sec3.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = inst.Gold });
        foreach (var (avatarId, pos, uniqueId, component) in GridFightInstance.InitialBenchRoles)
        {
            sec3.UpdateDynamicList.Add(new GridFightSyncData
            {
                AddRoleInfo = new GridGameRoleInfo { Id = avatarId, Pos = pos, RoleStar = 1, UniqueId = uniqueId, GridFightValueInitComponent = { [component] = 0 } }
            });
        }
        sync.SyncResultDataList.Add(sec3);

        var sec4 = new GridFightSyncResultData();
        sec4.UpdateDynamicList.Add(new GridFightSyncData { FinishPendingActionPos = ackPos });
        sec4.UpdateDynamicList.Add(new GridFightSyncData { SyncLockInfo = new GridFightLockInfo() });
        sync.SyncResultDataList.Add(sec4);

        var sec5 = new GridFightSyncResultData();
        sec5.UpdateDynamicList.Add(new GridFightSyncData
        {
            LevelSyncInfo = new GridFightLevelSyncInfo
            {
                DCPKPNLKGMM = inst.Level,
                SectionId = inst.SectionId,
                GridFightLayerInfo = new GridFightLayerInfo
                {
                    RouteInfo = new GridFightRouteInfo
                    {
                        FightCampId = 20,
                        EliteBranchId = 1,
                        RouteEncounterList =
                        {
                            new GridFightEncounterInfo
                            {
                                LFKBMDHKPFI = inst.BattleComponent.StageId,
                                MonsterWaveList =
                                {
                                    new GridEncounterMonsterWave
                                    {
                                        IGMMPDDCJIN = 1,
                                        PPOEDDFFEKK =
                                        {
                                            inst.BattleComponent.MonsterIds.Select(x => new PJLBDMPEKFP { MonsterId = x, RoleStar = 1 })
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        });
        sync.SyncResultDataList.Add(sec5);

        var sec6 = new GridFightSyncResultData();
        sec6.UpdateDynamicList.Add(new GridFightSyncData
        {
            SyncLockInfo = new GridFightLockInfo
            {
                LockReason = GridFightLockReason.DfofffceffoKjmjdbjmbmc,
                LockType = GridFightLockType.PjbmhhnlclbEhfhdgpocnh
            }
        });
        sec6.UpdateDynamicList.Add(new GridFightSyncData { PendingAction = inst.PendingAction });
        sync.SyncResultDataList.Add(sec6);
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(sync));
    }

    private static async System.Threading.Tasks.Task HandleRoundBegin(Connection connection, GridFightInstance inst, uint ackPos)
    {
        inst.RotateShop();
        var nextPos = ackPos + 8;
        inst.QueuePosition = nextPos;
        inst.PendingAction = new GridFightPendingAction
        {
            QueuePosition = nextPos,
            ReturnPreparationAction = new GridFightReturnPreparationActionInfo()
        };
        await connection.SendPacket(new PacketGridFightHandlePendingActionScRsp(ackPos));
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!, GridFightSyncKind.PendingAdvance, (ackPos, nextPos)));
    }

    private static async System.Threading.Tasks.Task HandleReturnPreparation(Connection connection, GridFightInstance inst, uint ackPos)
    {
        var nextPos = ackPos + 1;
        inst.QueuePosition = nextPos;
        inst.PendingAction = null;
        await connection.SendPacket(new PacketGridFightHandlePendingActionScRsp(ackPos));
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!, GridFightSyncKind.PendingAdvance, (ackPos, nextPos)));
    }
}
