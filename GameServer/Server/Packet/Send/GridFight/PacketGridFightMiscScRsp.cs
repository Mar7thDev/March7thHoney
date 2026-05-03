using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

public class PacketJNNNJBAOCOB : BasePacket { public PacketJNNNJBAOCOB() : base(CmdIds.JNNNJBAOCOB) => SetData(new JNNNJBAOCOB()); }
public class PacketEPJJBNPIFLC : BasePacket { public PacketEPJJBNPIFLC() : base(CmdIds.EPJJBNPIFLC) => SetData(new EPJJBNPIFLC()); }
public class PacketOLOGIALOJDP : BasePacket { public PacketOLOGIALOJDP() : base(CmdIds.OLOGIALOJDP) => SetData(new OLOGIALOJDP()); }
public class PacketEEBMIAFNJMC : BasePacket { public PacketEEBMIAFNJMC() : base(CmdIds.EEBMIAFNJMC) => SetData(new EEBMIAFNJMC()); }
public class PacketDJCHCHCAJPB : BasePacket { public PacketDJCHCHCAJPB() : base(CmdIds.DJCHCHCAJPB) => SetData(new DJCHCHCAJPB()); }
public class PacketHGOIBDBMDBG : BasePacket { public PacketHGOIBDBMDBG() : base(CmdIds.HGOIBDBMDBG) => SetData(new HGOIBDBMDBG()); }
public class PacketGridFightRecycleRoleScRsp : BasePacket { public PacketGridFightRecycleRoleScRsp() : base(CmdIds.GridFightRecycleRoleScRsp) => SetData(new GridFightRecycleRoleScRsp()); }
public class PacketGridFightUpdateEquipTrackScRsp : BasePacket { public PacketGridFightUpdateEquipTrackScRsp() : base(CmdIds.GridFightUpdateEquipTrackScRsp) => SetData(new GridFightUpdateEquipTrackScRsp()); }
