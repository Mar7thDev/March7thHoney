using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Gacha;

public class PacketSetGachaDecideItemScRsp : BasePacket
{
    public PacketSetGachaDecideItemScRsp(uint gachaId, List<uint> order) : base(CmdIds.SetGachaDecideItemScRsp)
    {
        var proto = new SetGachaDecideItemScRsp
        {
            LECPJJAMNPF = new OEIEJHBCOOM
            {
                KIFIEAKAJCK = gachaId,
                DGOMHDMJHEK = { order },
                MBOEFLAHLEM = 1
            }
        };

        SetData(proto);
    }
}

