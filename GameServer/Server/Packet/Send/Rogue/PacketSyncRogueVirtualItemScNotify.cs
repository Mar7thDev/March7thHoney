using March7thHoney.GameServer.Game.Rogue;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

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