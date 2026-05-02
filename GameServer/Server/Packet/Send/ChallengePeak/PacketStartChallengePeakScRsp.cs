using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChallengePeak;

public class PacketStartChallengePeakScRsp : BasePacket
{
    public PacketStartChallengePeakScRsp(Retcode retcode) : base(CmdIds.StartChallengePeakScRsp)
    {
        var proto = new StartChallengePeakScRsp
        {
            Retcode = (uint)retcode
        };

        SetData(proto);
    }
}