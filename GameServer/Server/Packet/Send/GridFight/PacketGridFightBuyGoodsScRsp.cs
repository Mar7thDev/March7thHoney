using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

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