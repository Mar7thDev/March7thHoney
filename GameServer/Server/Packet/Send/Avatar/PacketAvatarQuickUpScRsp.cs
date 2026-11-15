using March7thHoney.Database.Inventory;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Avatar;

public class PacketAvatarQuickUpScRsp : BasePacket
{
    public PacketAvatarQuickUpScRsp(Retcode retcode, List<ItemData> returnItems) : base(CmdIds.MILFFHPMLMN)
    {
        var proto = new PMEMIDKDAFG { Retcode = (uint)retcode };
        proto.ReturnItemList.AddRange(returnItems.Select(item => item.ToPileProto()));
        SetData(proto);
    }
}
