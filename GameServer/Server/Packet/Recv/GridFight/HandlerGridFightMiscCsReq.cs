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

        var (removedOrbUniqueIds, levelChanged, goldChanged) = inst.TryUseOrbs(req.HDFJAINBKJG, req.IsGetAll);

        var sync = new GridFightSyncUpdateResultScNotify();
        var sec = new GridFightSyncResultData();
        foreach (var uniqueId in removedOrbUniqueIds)
            sec.UpdateDynamicList.Add(new GridFightSyncData { RemoveOrbUniqueId = uniqueId });

        if (goldChanged)
            sec.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = inst.Gold });

        if (levelChanged)
        {
            sec.UpdateDynamicList.Add(new GridFightSyncData
            {
                PlayerLevel = new GridFightPlayerLevelSyncInfo
                {
                    Level = inst.PlayerLevel,
                    Exp = inst.PlayerExp,
                    MaxLevel = inst.PlayerMaxLevel
                }
            });
            sec.UpdateDynamicList.Add(new GridFightSyncData
            {
                GridFightBuyExpCost = inst.GetBuyExpCost()
            });
            sec.UpdateDynamicList.Add(new GridFightSyncData
            {
                MaxBattleRoleNum = inst.GetCurrentMaxBattleRoleNum()
            });
        }

        // 防止客户端面板卡住：即使本次没有实际消耗到 orb，也回一帧同步结果。
        if (sec.UpdateDynamicList.Count == 0)
            sec.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = inst.Gold });

        sync.SyncResultDataList.Add(sec);
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
