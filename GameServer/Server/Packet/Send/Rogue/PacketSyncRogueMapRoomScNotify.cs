using March7thHoney.GameServer.Game.Rogue;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketSyncRogueMapRoomScNotify : BasePacket
{
    public PacketSyncRogueMapRoomScNotify(RogueInstance inst, RogueRoomStatus? statusOverride = null) : base(
        CmdIds.SyncRogueMapRoomScNotify)
    {
        var proto = new SyncRogueMapRoomScNotify { MapId = (uint)inst.MapId };
        var room = inst.GetCurrentRoom();
        if (room != null) proto.HNEANMNJHDB = RogueManager.BuildRoom(room, statusOverride ?? room.Status);

        SetData(proto);
    }
}
