using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Quest;

public class PacketFinishQuestScRsp : BasePacket
{
    public PacketFinishQuestScRsp(Retcode retCode) : base(CmdIds.FinishQuestScRsp)
    {
        var proto = new FinishQuestScRsp
        {
            Retcode = (uint)retCode
        };

        SetData(proto);
    }
}