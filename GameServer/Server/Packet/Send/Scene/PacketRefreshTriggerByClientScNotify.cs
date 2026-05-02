using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;

public class PacketRefreshTriggerByClientScNotify : BasePacket
{
    public PacketRefreshTriggerByClientScNotify(string triggerName, uint entityId, List<uint> targetIds) : base(
        CmdIds.RefreshTriggerByClientScNotify)
    {
        var rsp = new RefreshTriggerByClientScNotify
        {
            TriggerName = triggerName,
            TriggerEntityId = entityId,
            TriggerTargetIdList = { targetIds }
        };

        SetData(rsp);
    }
}