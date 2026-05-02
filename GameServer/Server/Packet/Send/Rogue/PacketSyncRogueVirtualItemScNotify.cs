using CastoriceHoney.Server.GameServer.Game.Rogue;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;

public class PacketSyncRogueVirtualItemScNotify : BasePacket
{
    public PacketSyncRogueVirtualItemScNotify(BaseRogueInstance instance) : base(
        CmdIds.SyncRogueVirtualItemInfoScNotify)
    {
        var proto = new SyncRogueVirtualItemInfoScNotify
        {
            RogueVirtualItemInfo = new RogueVirtualItemInfo
            {
                //RogueMoney = (uint)instance.CurMoney,
            }
        };

        SetData(proto);
    }
}