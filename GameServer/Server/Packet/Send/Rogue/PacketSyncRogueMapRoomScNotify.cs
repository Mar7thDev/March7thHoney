using March7thHoney.GameServer.Game.Rogue.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketSyncRogueMapRoomScNotify : BasePacket
{
    public PacketSyncRogueMapRoomScNotify(RogueRoomInstance room, int mapId) : base(CmdIds.SyncRogueMapRoomScNotify)
    {
        var proto = new SyncRogueMapRoomScNotify
        {
            CurRoom = room.ToProto(),
            MapId = (uint)mapId
        };

        SetData(proto);
    }
}