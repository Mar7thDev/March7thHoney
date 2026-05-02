using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightBuyGoodsScRsp : BasePacket
{
    public PacketGridFightBuyGoodsScRsp(Retcode retcode) : base(CmdIds.GridFightBuyGoodsScRsp)
    {
        var rsp = new GridFightBuyGoodsScRsp
        {
            Retcode = (uint)retcode
        };

        SetData(rsp);
    }
}