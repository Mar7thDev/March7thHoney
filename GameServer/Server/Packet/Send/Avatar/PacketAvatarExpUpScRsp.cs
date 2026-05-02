using CastoriceHoney.Server.Database.Inventory;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Avatar;

public class PacketAvatarExpUpScRsp : BasePacket
{
    public PacketAvatarExpUpScRsp(List<ItemData> returnItem) : base(CmdIds.AvatarExpUpScRsp)
    {
        var proto = new AvatarExpUpScRsp();
        proto.ReturnItemList.AddRange(returnItem.Select(item => item.ToPileProto()));

        SetData(proto);
    }
}