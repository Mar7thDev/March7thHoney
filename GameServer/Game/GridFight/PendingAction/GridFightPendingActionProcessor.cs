using March7thHoney.Data;
using March7thHoney.GameServer.Game.GridFight.Sync;
using March7thHoney.GameServer.Game.GridFight.Battle;
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
        var newBuffList = inst.RollPortalBuffs(exclude: inst.CurrentPortalBuffOffer.ToList());
        inst.QueuePosition = ackPos;
        inst.PendingAction = new GridFightPendingAction
        {
            QueuePosition = ackPos,
            PortalBuffAction = new GridFightPortalBuffActionInfo { FCHPJKAIBHB = 1, GridFightPortalBuffList = { newBuffList } }
        };

        var notify = new GridFightSeasonHandBookNotify { HandbookGridFightPortalInfo = new GridFightHandBookPortalInfo() };
        foreach (var buffId in newBuffList) notify.HandbookGridFightPortalInfo.PELJLONLDNM.Add(buffId);
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
        inst.ClearPortalBuffOffer();

        uint requiredTrait = 0;
        if (GameData.GridFightPortalBuffData.TryGetValue(buffId, out var buffData)
            && buffData.PortalGameRefTrait.Count > 0)
        {
            requiredTrait = buffData.PortalGameRefTrait[0];
        }
        inst.MaterializeInitialBenchTeam(requiredTrait);
        var encounter = GridFightLevelResolver.Resolve(inst);
        inst.ConfigureNextBattle(encounter.StageId, encounter.Monsters.Select(m => m.MonsterId));

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

        var grantedEquipIds = new List<uint>();
        if (GameData.GridFightPortalBuffData.TryGetValue(buffId, out var portalBuffData))
        {
            // 抓包确认：buff 派的是 ShowBonusIDList 里的星徽，EffectParamList 是另一码事
            foreach (var bonusId in portalBuffData.ShowBonusIDList)
            {
                if (GameData.GridFightEquipmentData.ContainsKey(bonusId))
                    grantedEquipIds.Add(bonusId);
            }
        }
        if (grantedEquipIds.Count > 0)
        {
            var sec0 = new GridFightSyncResultData { GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpPnejelgglej };
            sec0.SyncEffectParamList.Add(buffId);
            sec0.SyncEffectParamList.Add(0u);
            var addItemInfo = new GridFightGameItemSyncInfo();
            foreach (var equipId in grantedEquipIds)
            {
                addItemInfo.GridFightEquipmentList.Add(new GridFightEquipmentInfo
                {
                    GridFightEquipmentId = equipId,
                    Source = 1,
                    UniqueId = inst.AllocEquipUniqueId()
                });
            }
            sec0.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = addItemInfo });
            sync.SyncResultDataList.Add(sec0);
        }

        var sec2 = new GridFightSyncResultData { GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpHndkhmefaal };
        sec2.UpdateDynamicList.Add(new GridFightSyncData { PortalServerDataUpdate = new GridFightPortalServerDataUpdate { PortalBuffId = buffId } });
        sync.SyncResultDataList.Add(sec2);

        var sec3 = new GridFightSyncResultData { GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpBjdeaahibge };
        sec3.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = inst.Gold });
        foreach (var (avatarId, pos, uniqueId, component) in inst.RolledBenchRoles)
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
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!, GridFightSyncKind.RefreshShop));
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
