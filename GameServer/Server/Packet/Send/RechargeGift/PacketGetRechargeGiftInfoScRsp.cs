using March7thHoney.Data;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RechargeGift;

public class PacketGetRechargeGiftInfoScRsp : BasePacket
{
    public PacketGetRechargeGiftInfoScRsp() : base(CmdIds.GetRechargeGiftInfoScRsp)
    {
        var proto = new GetRechargeGiftInfoScRsp();

        SetData(proto);
    }
}

