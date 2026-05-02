using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Recommend;

public class PacketRelicSmartWearGetPinRelicScRsp : BasePacket
{
    public PacketRelicSmartWearGetPinRelicScRsp(uint avatarId) : base(CmdIds.RelicSmartWearGetPinRelicScRsp)
    {
        var proto = new RelicSmartWearGetPinRelicScRsp
        {
            AvatarId = avatarId
        };

        SetData(proto);
    }
}