using CastoriceHoney.Server.GameServer.Game.RogueMagic.Adventure;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueCommon;

public class PacketSyncRogueAdventureRoomInfoScNotify : BasePacket
{
    public PacketSyncRogueAdventureRoomInfoScNotify(RogueMagicAdventureInstance instance) : base(
        CmdIds.SyncRogueAdventureRoomInfoScNotify)
    {
        var proto = new SyncRogueAdventureRoomInfoScNotify
        {
            AdventureRoomInfo = instance.ToProto()
        };

        SetData(proto);
    }
}