using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Gacha;

public class PacketSetGachaDecideItemScRsp : BasePacket
{
    public PacketSetGachaDecideItemScRsp(uint gachaId, uint decideItemType, List<uint> order,
        Retcode retcode = Retcode.RetSucc) : base(CmdIds.SetGachaDecideItemScRsp)
    {
        var proto = new SetGachaDecideItemScRsp
        {
            Retcode = (uint)retcode,
            LECPJJAMNPF = new OEIEJHBCOOM
            {
                PAPOKACIPPJ = gachaId,
                DGOMHDMJHEK = { order },
                MBOEFLAHLEM = decideItemType
            }
        };

        SetData(proto);
    }
}

