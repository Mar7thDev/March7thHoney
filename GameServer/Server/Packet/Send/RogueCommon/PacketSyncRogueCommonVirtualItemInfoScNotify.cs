using CastoriceHoney.Server.GameServer.Game.Rogue;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueCommon;

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