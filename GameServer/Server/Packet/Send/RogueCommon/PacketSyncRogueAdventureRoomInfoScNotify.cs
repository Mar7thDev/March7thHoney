using March7thHoney.GameServer.Game.RogueMagic.Adventure;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueCommon;

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