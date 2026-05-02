using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Fight;

public class PacketFightGeneralScRsp : BasePacket
{
    public PacketFightGeneralScRsp(uint networkType) : base(CmdIds.FightGeneralScRsp)
    {
        var proto = new FightGeneralScRsp
        {
            NetworkMsgType = networkType
        };

        SetData(proto);
    }

    public PacketFightGeneralScRsp(Retcode code) : base(CmdIds.FightGeneralScRsp)
    {
        var proto = new FightGeneralScRsp
        {
            Retcode = (uint)code
        };

        SetData(proto);
    }
}