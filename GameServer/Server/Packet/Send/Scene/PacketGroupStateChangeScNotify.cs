using CastoriceHoney.Server.Enums.Scene;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;

public class PacketGroupStateChangeScNotify : BasePacket
{
    public PacketGroupStateChangeScNotify(int entryId, int groupId, PropStateEnum propState) : base(
        CmdIds.GroupStateChangeScNotify)
    {
        var notify = new GroupStateChangeScNotify
        {
            GroupStateInfo = new GroupStateInfo
            {
                EntryId = (uint)entryId,
                GroupId = (uint)groupId,
                GroupState = (uint)propState
            }
        };

        SetData(notify);
    }
}