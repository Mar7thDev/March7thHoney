using March7thHoney.Data;
using March7thHoney.GameServer.Game.GridFight.Sync;
using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightEquipDressCsReq)]
public class HandlerGridFightEquipDressCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightEquipDressCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var inst = player.GridFightManager?.GridFightInstance;

        await connection.SendPacket(new PacketGridFightEquipDressScRsp());
        if (inst == null) return;

        var equipUid = req.DressEquipmentUniqueId;
        var roleUid = req.DressRoleUniqueId;
        if (!inst.RoleByUniqueId.TryGetValue(roleUid, out var roleId)) return;
        if (!inst.Equipments.Any(e => e.UniqueId == equipUid)) return;

        if (!inst.EquipUniqueIdsByRoleUniqueId.TryGetValue(roleUid, out var equipList))
            inst.EquipUniqueIdsByRoleUniqueId[roleUid] = equipList = new List<uint>();
        if (!equipList.Contains(equipUid)) equipList.Add(equipUid);

        uint pos = 0;
        foreach (var kv in inst.UniqueIdByPos)
            if (kv.Value == roleUid) { pos = kv.Key; break; }

        var roleInfo = new GridGameRoleInfo
        {
            Id = roleId,
            Pos = pos,
            UniqueId = roleUid,
            RoleStar = inst.RoleStarByUniqueId.GetValueOrDefault(roleUid, 1u)
        };
        foreach (var eid in equipList) roleInfo.UpdateEquipsComponent.Add(eid);
        if (GameData.GridFightRoleBasicInfoData.TryGetValue(roleId, out var roleExcel)
            && roleExcel.RoleSavedValueList.Count > 0)
        {
            roleInfo.GridFightValueInitComponent[roleExcel.RoleSavedValueList[0]] = 0;
        }

        var sync = new GridFightSyncUpdateResultScNotify();
        var sec = new GridFightSyncResultData();
        sec.UpdateDynamicList.Add(new GridFightSyncData { UpdateRoleInfo = roleInfo });
        sync.SyncResultDataList.Add(sec);
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(sync));
    }
}

[Opcode(CmdIds.GridFightEquipCraftCsReq)]
public class HandlerGridFightEquipCraftCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightEquipCraftCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketEPJJBNPIFLC()); await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!, GridFightSyncKind.NoOp)); } }

[Opcode(CmdIds.GridFightUseConsumableCsReq)]
public class HandlerGridFightUseConsumableCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightUseConsumableCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketOLOGIALOJDP()); await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!, GridFightSyncKind.NoOp)); } }

[Opcode(CmdIds.GridFightUseForgeCsReq)]
public class HandlerGridFightUseForgeCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightUseForgeCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketEEBMIAFNJMC()); await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!, GridFightSyncKind.NoOp)); } }

[Opcode(CmdIds.GridFightBuyExpCsReq)]
public class HandlerGridFightBuyExpCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightBuyExpCsReq.Parser.ParseFrom(data); var player = connection.Player!; _ = new GridFightService(player).BuyExp(); await connection.SendPacket(new PacketDJCHCHCAJPB()); await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(player, GridFightSyncKind.BuyExp)); } }

[Opcode(CmdIds.GridFightLockShopCsReq)]
public class HandlerGridFightLockShopCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightLockShopCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketHGOIBDBMDBG()); await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!, GridFightSyncKind.NoOp)); } }

[Opcode(CmdIds.GJMIIBDEAAJ)]
public class HandlerGridFightUseOrbCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GJMIIBDEAAJ.Parser.ParseFrom(data);
        var player = connection.Player!;
        var inst = player.GridFightManager?.GridFightInstance;

        await connection.SendPacket(new PacketCEFIMADBIBH());
        if (inst == null) return;

        var results = inst.TryUseOrbsDetailed(req.HDFJAINBKJG, req.IsGetAll);

        var sync = new GridFightSyncUpdateResultScNotify();
        var levelChanged = false;

        // 抓包：每颗晶矿单独一段 SyncResultData，gridUpdateSrc=OOPNBCIJHMP，
        // syncEffectParamList=[uniqueId, itemId]，里面是 removeOrbUniqueId + 对应奖励。
        // 客户端依赖这个分段格式播放收集动画并隐藏晶矿。
        foreach (var r in results)
        {
            if (r.LevelChanged) levelChanged = true;

            var sec = new GridFightSyncResultData
            {
                GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpOopnbcijhmp
            };
            sec.SyncEffectParamList.Add(r.UniqueId);
            sec.SyncEffectParamList.Add(r.ItemId);
            sec.UpdateDynamicList.Add(new GridFightSyncData { RemoveOrbUniqueId = r.UniqueId });

            if (r.AddEquipmentId.HasValue && r.AddEquipmentUniqueId.HasValue)
            {
                var item = new GridFightGameItemSyncInfo();
                item.GridFightEquipmentList.Add(new GridFightEquipmentInfo
                {
                    GridFightEquipmentId = r.AddEquipmentId.Value,
                    Source = 1,
                    UniqueId = r.AddEquipmentUniqueId.Value
                });
                sec.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = item });
            }

            if (r.AddConsumableItemId.HasValue)
            {
                var item = new GridFightGameItemSyncInfo();
                item.UpdateGridFightConsumableList.Add(new GridFightConsumableUpdateInfo
                {
                    ItemId = r.AddConsumableItemId.Value,
                    ItemStackCount = 1,
                    Num = 1
                });
                sec.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = item });
            }

            if (r.GoldChanged)
                sec.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = r.GoldAfter });

            if (r.AddRoleId.HasValue && r.AddRoleUniqueId.HasValue && r.AddRolePos.HasValue)
            {
                var roleInfo = new GridGameRoleInfo
                {
                    Id = r.AddRoleId.Value,
                    Pos = r.AddRolePos.Value,
                    RoleStar = 1,
                    UniqueId = r.AddRoleUniqueId.Value
                };
                if (!string.IsNullOrEmpty(r.AddRoleComponent))
                    roleInfo.GridFightValueInitComponent[r.AddRoleComponent] = 0;
                sec.UpdateDynamicList.Add(new GridFightSyncData { AddRoleInfo = roleInfo });
            }

            sync.SyncResultDataList.Add(sec);
        }

        if (levelChanged)
        {
            var lvlSec = new GridFightSyncResultData();
            lvlSec.UpdateDynamicList.Add(new GridFightSyncData
            {
                PlayerLevel = new GridFightPlayerLevelSyncInfo
                {
                    Level = inst.PlayerLevel,
                    Exp = inst.PlayerExp,
                    MaxLevel = inst.PlayerMaxLevel
                }
            });
            lvlSec.UpdateDynamicList.Add(new GridFightSyncData { GridFightBuyExpCost = inst.GetBuyExpCost() });
            lvlSec.UpdateDynamicList.Add(new GridFightSyncData { MaxBattleRoleNum = inst.GetCurrentMaxBattleRoleNum() });
            sync.SyncResultDataList.Add(lvlSec);
        }

        // 没有任何 orb 被消耗时回一帧空结果，避免客户端面板卡住
        if (sync.SyncResultDataList.Count == 0)
        {
            var noop = new GridFightSyncResultData();
            noop.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = inst.Gold });
            sync.SyncResultDataList.Add(noop);
        }

        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(sync));
    }
}

[Opcode(CmdIds.GridFightRecycleRoleCsReq)]
public class HandlerGridFightRecycleRoleCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightRecycleRoleCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var refund = new GridFightService(player).RecycleRole(req.UniqueId);
        await connection.SendPacket(new PacketGridFightRecycleRoleScRsp());
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(player, GridFightSyncKind.RecycleRole, (req.UniqueId, (int)refund)));
    }
}

[Opcode(CmdIds.GridFightUpdateEquipTrackCsReq)]
public class HandlerGridFightUpdateEquipTrackCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightUpdateEquipTrackCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketGridFightUpdateEquipTrackScRsp()); } }
