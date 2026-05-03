using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightEquipDressCsReq)]
public class HandlerGridFightEquipDressCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightEquipDressCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketJNNNJBAOCOB()); } }

[Opcode(CmdIds.GridFightEquipCraftCsReq)]
public class HandlerGridFightEquipCraftCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightEquipCraftCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketEPJJBNPIFLC()); } }

[Opcode(CmdIds.GridFightUseConsumableCsReq)]
public class HandlerGridFightUseConsumableCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightUseConsumableCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketOLOGIALOJDP()); await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!)); } }

[Opcode(CmdIds.GridFightUseForgeCsReq)]
public class HandlerGridFightUseForgeCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightUseForgeCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketEEBMIAFNJMC()); await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!)); } }

[Opcode(CmdIds.GridFightBuyExpCsReq)]
public class HandlerGridFightBuyExpCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightBuyExpCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketDJCHCHCAJPB()); await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!)); } }

[Opcode(CmdIds.GridFightLockShopCsReq)]
public class HandlerGridFightLockShopCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightLockShopCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketHGOIBDBMDBG()); await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!)); } }

[Opcode(CmdIds.GridFightRecycleRoleCsReq)]
public class HandlerGridFightRecycleRoleCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightRecycleRoleCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketGridFightRecycleRoleScRsp()); await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!)); } }

[Opcode(CmdIds.GridFightUpdateEquipTrackCsReq)]
public class HandlerGridFightUpdateEquipTrackCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightUpdateEquipTrackCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketGridFightUpdateEquipTrackScRsp()); } }
