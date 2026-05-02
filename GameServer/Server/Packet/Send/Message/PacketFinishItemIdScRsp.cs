using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Message;

public class PacketFinishItemIdScRsp : BasePacket
{
    public PacketFinishItemIdScRsp(uint itemId) : base(CmdIds.FinishItemIdScRsp)
    {
        var proto = new FinishItemIdScRsp
        {
            ItemId = itemId
        };
        SetData(proto);
    }
}