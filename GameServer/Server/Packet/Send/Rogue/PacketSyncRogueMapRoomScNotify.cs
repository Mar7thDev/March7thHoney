using CastoriceHoney.Server.GameServer.Game.Rogue.Scene;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;

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