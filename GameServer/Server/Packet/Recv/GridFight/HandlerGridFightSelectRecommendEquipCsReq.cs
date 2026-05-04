using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightSelectRecommendEquipCsReq)]
public class HandlerGridFightSelectRecommendEquipCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightSelectRecommendEquipCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var inst = player.GridFightManager?.GridFightInstance;
        var ackPos = req.QueuePosition;

        if (inst == null)
        {
            await connection.SendPacket(new PacketGridFightHandlePendingActionScRsp(ackPos));
            await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(player));
            return;
        }

        if (req.PortalBuffRerollAction != null)
        {
            await HandlePortalBuffReroll(connection, inst, ackPos);
        }
        else if (req.PortalBuffAction != null)
        {
            await HandlePortalBuffSelect(connection, inst, ackPos, req.PortalBuffAction.SelectPortalBuffId);
        }
        else if (req.RoundBeginAction != null)
        {
            await HandleRoundBegin(connection, inst, ackPos);
        }
        else if (req.ReturnPreparationAction != null)
        {
            await HandleReturnPreparation(connection, inst, ackPos);
        }
        else
        {
            // 其他 action 类型：默认 +1 + RoundBegin
            inst.QueuePosition = ackPos + 1;
            inst.PendingAction = new GridFightPendingAction
            {
                QueuePosition = inst.QueuePosition,
                RoundBeginAction = new GridFightRoundBeginActionInfo()
            };
            await connection.SendPacket(new PacketGridFightHandlePendingActionScRsp(ackPos));
            await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(player, GridFightSyncKind.PendingAdvance, (ackPos, inst.QueuePosition)));
        }
    }

    // PortalBuff 重抽：队列不推进，仅切换 action 类型为 PortalBuffAction（让客户端选 buff）
    private static async Task HandlePortalBuffReroll(Connection connection, GridFightInstance inst, uint ackPos)
    {
        var newBuffList = new List<uint> { 105u, 1014u, 132u };
        inst.QueuePosition = ackPos;
        inst.PendingAction = new GridFightPendingAction
        {
            QueuePosition = ackPos,
            PortalBuffAction = new GridFightPortalBuffActionInfo
            {
                FCHPJKAIBHB = 1,
                GridFightPortalBuffList = { newBuffList }
            }
        };

        var notify = new GridFightSeasonHandBookNotify { HandbookGridFightPortalInfo = new GridFightHandBookPortalInfo() };
        notify.HandbookGridFightPortalInfo.PELJLONLDNM.Add(132u);
        await connection.SendPacket(new PacketGridFightSeasonHandBookNotify(notify));

        await connection.SendPacket(new PacketGridFightHandlePendingActionScRsp(ackPos));

        // Sync：仅一段，新 PendingAction（同 queuePos）
        var sync = new GridFightSyncUpdateResultScNotify();
        var pendingSection = new GridFightSyncResultData();
        pendingSection.UpdateDynamicList.Add(new GridFightSyncData { PendingAction = inst.PendingAction });
        sync.SyncResultDataList.Add(pendingSection);
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(sync));
    }

    // PortalBuff 选择：抓包 seq 423 7-section sync — 团队实例化 + buff 应用 + 关卡加载 + 队列推进
    private static async Task HandlePortalBuffSelect(Connection connection, GridFightInstance inst, uint ackPos, uint buffId)
    {
        if (buffId > 0 && !inst.ActivePortalBuffIds.Contains(buffId)) inst.ActivePortalBuffIds.Add(buffId);
        inst.MaterializeInitialBenchTeam();

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

        // 构造完整的 7-section sync（参考抓包 seq 423）
        var sync = new GridFightSyncUpdateResultScNotify();

        // section 0: addGameItemInfo（送 1 件起始装备）
        var equipUid = inst.AllocEquipUniqueId();
        var sec0 = new GridFightSyncResultData { GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpPnejelgglej };
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

        // section 1: addForgeInfo（锻造台落到 pos:14）
        var forgeUid = inst.AllocEquipUniqueId();
        var sec1 = new GridFightSyncResultData { GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpPnejelgglej };
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

        // section 2: portalServerDataUpdate（确认 buff 已应用）
        var sec2 = new GridFightSyncResultData { GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpHndkhmefaal };
        sec2.UpdateDynamicList.Add(new GridFightSyncData
        {
            PortalServerDataUpdate = new GridFightPortalServerDataUpdate { PortalBuffId = buffId }
        });
        sync.SyncResultDataList.Add(sec2);

        // section 3: itemValue + 4 addRoleInfo（金币 + 团队实例化）
        var sec3 = new GridFightSyncResultData { GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpBjdeaahibge };
        sec3.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = inst.Gold });
        foreach (var (avatarId, pos, uniqueId, component) in GridFightInstance.InitialBenchRoles)
        {
            sec3.UpdateDynamicList.Add(new GridFightSyncData
            {
                AddRoleInfo = new GridGameRoleInfo
                {
                    Id = avatarId,
                    Pos = pos,
                    RoleStar = 1,
                    UniqueId = uniqueId,
                    GridFightValueInitComponent = { [component] = 0 }
                }
            });
        }
        sync.SyncResultDataList.Add(sec3);

        // section 4: finishPendingActionPos:1 + 空 syncLockInfo
        var sec4 = new GridFightSyncResultData();
        sec4.UpdateDynamicList.Add(new GridFightSyncData { FinishPendingActionPos = ackPos });
        sec4.UpdateDynamicList.Add(new GridFightSyncData { SyncLockInfo = new GridFightLockInfo() });
        sync.SyncResultDataList.Add(sec4);

        // section 5: levelSyncInfo（加载下一战的怪物波）
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
                                LFKBMDHKPFI = 90303,
                                MonsterWaveList =
                                {
                                    new GridEncounterMonsterWave
                                    {
                                        IGMMPDDCJIN = 1,
                                        PPOEDDFFEKK =
                                        {
                                            new PJLBDMPEKFP { MonsterId = 300101007, RoleStar = 1 },
                                            new PJLBDMPEKFP { MonsterId = 800102021, RoleStar = 1 },
                                            new PJLBDMPEKFP { MonsterId = 300101007, RoleStar = 1 }
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

        // section 6: 锁定 syncLockInfo + 新 pendingAction
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

    // RoundBegin：商店解锁 + 队列大跳（+8）+ 新 pending = ReturnPreparation
    private static async Task HandleRoundBegin(Connection connection, GridFightInstance inst, uint ackPos)
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

    // ReturnPreparation：等客户端发 EnterBattleStage
    private static async Task HandleReturnPreparation(Connection connection, GridFightInstance inst, uint ackPos)
    {
        var nextPos = ackPos + 1;
        inst.QueuePosition = nextPos;
        inst.PendingAction = null;
        await connection.SendPacket(new PacketGridFightHandlePendingActionScRsp(ackPos));
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!, GridFightSyncKind.PendingAdvance, (ackPos, nextPos)));
    }
}
