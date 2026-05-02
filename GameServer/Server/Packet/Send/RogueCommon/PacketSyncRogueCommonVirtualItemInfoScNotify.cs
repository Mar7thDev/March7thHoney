using March7thHoney.GameServer.Game.Rogue;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueCommon;

public class PacketSyncRogueCommonVirtualItemInfoScNotify : BasePacket
{
    public PacketSyncRogueCommonVirtualItemInfoScNotify(BaseRogueInstance instance) : base(
        CmdIds.SyncRogueCommonVirtualItemInfoScNotify)
    {
        var proto = new SyncRogueCommonVirtualItemInfoScNotify
        {
            CommonItemInfo =
            {
                new RogueCommonVirtualItemInfo
                {
                    VirtualItemId = 31,
                    VirtualItemNum = (uint)instance.CurMoney
                }
            }
        };

        SetData(proto);
    }
}