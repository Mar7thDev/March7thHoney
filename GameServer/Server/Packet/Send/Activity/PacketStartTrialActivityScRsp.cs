using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Activity;

public class PacketStartTrialActivityScRsp : BasePacket
{
    public PacketStartTrialActivityScRsp(uint stageId) : base(CmdIds.StartTrialActivityScRsp)
    {
        var proto = new StartTrialActivityScRsp
        {
            StageId = stageId
        };

        SetData(proto);
    }
}